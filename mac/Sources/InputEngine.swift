// Recording and playback.
//
// Windows used low-level hooks (WH_MOUSE_LL / WH_KEYBOARD_LL) plus SendInput.
// The macOS equivalents are a Quartz event tap and CGEvent.post. The idea that
// makes both work is the same: every event we synthesize is tagged, so the tap
// can tell the macro's own input apart from the user's.

import Foundation
import CoreGraphics
import ApplicationServices

/// Stamped onto every posted event — the counterpart of the Windows dwExtraInfo tag.
private let dolfTag: Int64 = 0x444F4C46  // 'DOLF'

final class InputEngine {

    static let shared = InputEngine()

    // MARK: State

    private var tap: CFMachPort?
    private var runLoopSource: CFRunLoopSource?

    private(set) var isRecording = false
    private(set) var isPlaying = false

    private var events: [MacroEvent] = []
    private var lastStamp: TimeInterval = 0
    private var lastPoint = CGPoint(x: -1, y: -1)

    /// Off records clicks only; each click still lands at the right spot.
    var recordMouseMoves = true
    /// A real mouse move during playback hands control back to the user.
    var stopOnMouseMove = true

    private var playThread: Thread?
    private var stopFlag = false
    private var heldButtons = Set<Int>()

    // MARK: Callbacks (delivered on the main queue)

    var onHotkeyRecord: (() -> Void)?
    var onHotkeyPlay: (() -> Void)?
    var onCancelPlayback: (() -> Void)?
    var onCaptureCountChanged: ((Int) -> Void)?
    var onLoopStarted: ((Int) -> Void)?
    var onProgress: ((Double) -> Void)?
    var onFinished: (() -> Void)?

    // MARK: - Permission

    /// Event taps require Accessibility. Passing true shows the system prompt once.
    @discardableResult
    static func hasAccessibility(prompting: Bool) -> Bool {
        let key = kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String
        let options = [key: prompting] as CFDictionary
        return AXIsProcessTrustedWithOptions(options)
    }

    // MARK: - Tap lifecycle

    @discardableResult
    func startTap() -> Bool {
        if tap != nil { return true }

        let types: [CGEventType] = [
            .mouseMoved,
            .leftMouseDown, .leftMouseUp, .leftMouseDragged,
            .rightMouseDown, .rightMouseUp, .rightMouseDragged,
            .otherMouseDown, .otherMouseUp, .otherMouseDragged,
            .scrollWheel,
            .keyDown, .keyUp, .flagsChanged
        ]
        var mask: CGEventMask = 0
        for t in types { mask |= (1 << UInt64(t.rawValue)) }

        let callback: CGEventTapCallBack = { _, type, event, refcon in
            guard let refcon = refcon else { return Unmanaged.passUnretained(event) }
            let engine = Unmanaged<InputEngine>.fromOpaque(refcon).takeUnretainedValue()
            return engine.handle(type: type, event: event)
        }

        guard let newTap = CGEvent.tapCreate(
            tap: .cgSessionEventTap,
            place: .headInsertEventTap,
            options: .defaultTap,          // not listen-only: F9/F10 are swallowed
            eventsOfInterest: mask,
            callback: callback,
            userInfo: Unmanaged.passUnretained(self).toOpaque()
        ) else {
            return false
        }

        tap = newTap
        runLoopSource = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, newTap, 0)
        CFRunLoopAddSource(CFRunLoopGetMain(), runLoopSource, .commonModes)
        CGEvent.tapEnable(tap: newTap, enable: true)
        return true
    }

    func stopTap() {
        if let source = runLoopSource {
            CFRunLoopRemoveSource(CFRunLoopGetMain(), source, .commonModes)
        }
        if let tap = tap {
            CGEvent.tapEnable(tap: tap, enable: false)
        }
        runLoopSource = nil
        tap = nil
    }

    // MARK: - Tap callback

    private func handle(type: CGEventType, event: CGEvent) -> Unmanaged<CGEvent>? {
        // The system disables a tap that takes too long; just switch it back on.
        if type == .tapDisabledByTimeout || type == .tapDisabledByUserInput {
            if let tap = tap { CGEvent.tapEnable(tap: tap, enable: true) }
            return Unmanaged.passUnretained(event)
        }

        let injected = event.getIntegerValueField(.eventSourceUserData) == dolfTag

        if !injected && type == .keyDown {
            let code = Int(event.getIntegerValueField(.keyboardEventKeycode))
            if code == KeyNames.f9 {
                DispatchQueue.main.async { self.onHotkeyRecord?() }
                return nil                      // consume, like RegisterHotKey did
            }
            if code == KeyNames.f10 {
                DispatchQueue.main.async { self.onHotkeyPlay?() }
                return nil
            }
            if code == KeyNames.escape && isPlaying {
                DispatchQueue.main.async { self.onCancelPlayback?() }
                return Unmanaged.passUnretained(event)
            }
        }

        // A real move means the user has taken the mouse back.
        if !injected && isPlaying && stopOnMouseMove
            && (type == .mouseMoved || type == .leftMouseDragged
                || type == .rightMouseDragged || type == .otherMouseDragged) {
            DispatchQueue.main.async { self.onCancelPlayback?() }
            return Unmanaged.passUnretained(event)
        }

        if isRecording && !injected {
            record(type: type, event: event)
        }

        return Unmanaged.passUnretained(event)
    }

    // MARK: - Recording

    func startRecording() {
        events.removeAll()
        lastStamp = ProcessInfo.processInfo.systemUptime
        lastPoint = CGPoint(x: -1, y: -1)
        isRecording = true
    }

    @discardableResult
    func stopRecording() -> [MacroEvent] {
        isRecording = false
        // The trip back to the Stop button isn't part of the macro.
        while let last = events.last, last.kind == .mouseMove {
            events.removeLast()
        }
        return events
    }

    var capturedEvents: [MacroEvent] { return events }

    private func nextDelay() -> Int {
        let now = ProcessInfo.processInfo.systemUptime
        let ms = Int(((now - lastStamp) * 1000.0).rounded())
        lastStamp = now
        return max(0, ms)
    }

    private func record(type: CGEventType, event: CGEvent) {
        let location = event.location

        switch type {
        case .mouseMoved, .leftMouseDragged, .rightMouseDragged, .otherMouseDragged:
            guard recordMouseMoves else { return }
            addMove(to: location)

        case .leftMouseDown, .leftMouseUp,
             .rightMouseDown, .rightMouseUp,
             .otherMouseDown, .otherMouseUp:
            // Pin the click to the exact spot even when moves aren't recorded.
            if location != lastPoint { addMove(to: location, force: true) }
            let down = (type == .leftMouseDown || type == .rightMouseDown || type == .otherMouseDown)
            let button = Int(event.getIntegerValueField(.mouseEventButtonNumber))
            events.append(MacroEvent(kind: .mouseButton, delay: nextDelay(),
                                     x: location.x, y: location.y,
                                     button: button, isDown: down))

        case .scrollWheel:
            let dy = Int(event.getIntegerValueField(.scrollWheelEventDeltaAxis1))
            let dx = Int(event.getIntegerValueField(.scrollWheelEventDeltaAxis2))
            events.append(MacroEvent(kind: .scroll, delay: nextDelay(),
                                     x: location.x, y: location.y,
                                     scrollY: dy, scrollX: dx))

        case .keyDown, .keyUp:
            let code = Int(event.getIntegerValueField(.keyboardEventKeycode))
            if code == KeyNames.f9 || code == KeyNames.f10 { return }
            events.append(MacroEvent(kind: .key, delay: nextDelay(),
                                     isDown: type == .keyDown,
                                     keyCode: code, flags: event.flags.rawValue))

        case .flagsChanged:
            // Command/Shift/Option/Control don't produce keyDown on macOS.
            let code = Int(event.getIntegerValueField(.keyboardEventKeycode))
            events.append(MacroEvent(kind: .flags, delay: nextDelay(),
                                     keyCode: code, flags: event.flags.rawValue))

        default:
            break
        }

        let count = events.count
        DispatchQueue.main.async { self.onCaptureCountChanged?(count) }
    }

    private func addMove(to point: CGPoint, force: Bool = false) {
        if !force && point == lastPoint { return }

        // Collapse a burst of moves so recordings stay small.
        if !force, let last = events.last, last.kind == .mouseMove {
            let sinceLast = (ProcessInfo.processInfo.systemUptime - lastStamp) * 1000.0
            if sinceLast < 8 {
                var updated = last
                updated.x = point.x
                updated.y = point.y
                events[events.count - 1] = updated
                lastPoint = point
                return
            }
        }

        events.append(MacroEvent(kind: .mouseMove, delay: nextDelay(), x: point.x, y: point.y))
        lastPoint = point
    }

    // MARK: - Playback

    /// - Parameters:
    ///   - repeatCount: 0 repeats until stopped.
    ///   - speed: 0 replays with no delays at all.
    func play(_ macro: [MacroEvent], repeatCount: Int, speed: Double) {
        guard !macro.isEmpty, !isPlaying else { return }
        stopFlag = false
        isPlaying = true
        heldButtons.removeAll()

        let thread = Thread { [weak self] in
            self?.runPlayback(macro, repeatCount: repeatCount, speed: speed)
        }
        thread.qualityOfService = .userInteractive
        playThread = thread
        thread.start()
    }

    func stopPlayback() {
        stopFlag = true
    }

    private func runPlayback(_ macro: [MacroEvent], repeatCount: Int, speed: Double) {
        // Let go of whatever started playback before the macro takes over.
        preciseSleep(250)

        var pass = 1
        while !stopFlag && (repeatCount == 0 || pass <= repeatCount) {
            let current = pass
            DispatchQueue.main.async { self.onLoopStarted?(current) }

            var lastPercent = -1
            for (index, event) in macro.enumerated() {
                if stopFlag { break }
                if speed > 0 && event.delay > 0 {
                    preciseSleep(Double(event.delay) / speed)
                }
                if stopFlag { break }
                send(event)

                let percent = (index + 1) * 100 / macro.count
                if percent != lastPercent {
                    lastPercent = percent
                    let fraction = Double(percent) / 100.0
                    DispatchQueue.main.async { self.onProgress?(fraction) }
                }
            }
            pass += 1
        }

        releaseHeldInput()
        isPlaying = false
        DispatchQueue.main.async { self.onFinished?() }
    }

    private func post(_ event: CGEvent) {
        event.setIntegerValueField(.eventSourceUserData, value: dolfTag)
        event.post(tap: .cghidEventTap)
    }

    private func send(_ e: MacroEvent) {
        let source = CGEventSource(stateID: .hidSystemState)
        let point = CGPoint(x: e.x, y: e.y)

        switch e.kind {
        case .mouseMove:
            // With a button held, apps expect a drag rather than a plain move.
            var type = CGEventType.mouseMoved
            var button = CGMouseButton.left
            if let held = heldButtons.first {
                button = CGMouseButton(rawValue: UInt32(held)) ?? .left
                switch held {
                case 1: type = .rightMouseDragged
                case 0: type = .leftMouseDragged
                default: type = .otherMouseDragged
                }
            }
            if let event = CGEvent(mouseEventSource: source, mouseType: type,
                                   mouseCursorPosition: point, mouseButton: button) {
                post(event)
            }

        case .mouseButton:
            let button = CGMouseButton(rawValue: UInt32(e.button)) ?? .left
            let type: CGEventType
            switch e.button {
            case 1: type = e.isDown ? .rightMouseDown : .rightMouseUp
            case 0: type = e.isDown ? .leftMouseDown : .leftMouseUp
            default: type = e.isDown ? .otherMouseDown : .otherMouseUp
            }
            if let event = CGEvent(mouseEventSource: source, mouseType: type,
                                   mouseCursorPosition: point, mouseButton: button) {
                event.setIntegerValueField(.mouseEventButtonNumber, value: Int64(e.button))
                post(event)
            }
            if e.isDown { heldButtons.insert(e.button) } else { heldButtons.remove(e.button) }

        case .scroll:
            if let event = CGEvent(scrollWheelEvent2Source: source, units: .line,
                                   wheelCount: 2,
                                   wheel1: Int32(e.scrollY), wheel2: Int32(e.scrollX), wheel3: 0) {
                post(event)
            }

        case .key:
            if let event = CGEvent(keyboardEventSource: source,
                                   virtualKey: CGKeyCode(e.keyCode), keyDown: e.isDown) {
                event.flags = CGEventFlags(rawValue: e.flags)
                post(event)
            }

        case .flags:
            // Replayed as a keyDown-shaped event carrying the new modifier state.
            if let event = CGEvent(keyboardEventSource: source,
                                   virtualKey: CGKeyCode(e.keyCode), keyDown: true) {
                event.type = .flagsChanged
                event.flags = CGEventFlags(rawValue: e.flags)
                post(event)
            }
        }
    }

    /// Never leave a button or modifier stuck down after an aborted run.
    private func releaseHeldInput() {
        let source = CGEventSource(stateID: .hidSystemState)
        let position = CGEvent(source: nil)?.location ?? .zero

        for held in heldButtons {
            let button = CGMouseButton(rawValue: UInt32(held)) ?? .left
            let type: CGEventType
            switch held {
            case 1: type = .rightMouseUp
            case 0: type = .leftMouseUp
            default: type = .otherMouseUp
            }
            if let event = CGEvent(mouseEventSource: source, mouseType: type,
                                   mouseCursorPosition: position, mouseButton: button) {
                post(event)
            }
        }
        heldButtons.removeAll()

        // Clear any modifiers the macro was holding.
        if let event = CGEvent(keyboardEventSource: source, virtualKey: 0, keyDown: true) {
            event.type = .flagsChanged
            event.flags = CGEventFlags(rawValue: 0)
            post(event)
        }
    }

    // MARK: - Timing

    /// Thread.sleep alone drifts badly over a long macro; sleep coarsely, then spin.
    private func preciseSleep(_ milliseconds: Double) {
        if milliseconds <= 0 { return }
        let end = ProcessInfo.processInfo.systemUptime + milliseconds / 1000.0
        while !stopFlag {
            let remaining = end - ProcessInfo.processInfo.systemUptime
            if remaining <= 0 { break }
            if remaining > 0.020 {
                Thread.sleep(forTimeInterval: 0.008)
            } else if remaining > 0.002 {
                Thread.sleep(forTimeInterval: 0.0005)
            }
            // else: spin out the last couple of milliseconds
        }
    }
}
