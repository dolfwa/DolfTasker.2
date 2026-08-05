// The main window: Record / Play, status well, and the three groups.
// Layout mirrors the Windows build — 380pt wide, 26pt controls, tight groups.

import AppKit

final class MainWindowController: NSWindowController {

    private let engine = InputEngine.shared

    private var events: [MacroEvent] = []
    private var currentURL: URL?
    private var playMeta = ""
    private var blinkTimer: Timer?
    private var blinkOn = false

    private var recordButton: DwButton!
    private var playButton: DwButton!
    private var actionsButton: DwButton!
    private var openButton: DwButton!
    private var saveButton: DwButton!
    private var statusView: DwStatusView!
    private var repeatField: NSTextField!
    private var repeatStepper: NSStepper!
    private var speedPopup: NSPopUpButton!
    private var movesCheck: NSButton!
    private var floatCheck: NSButton!
    private var stopOnMoveCheck: NSButton!
    private var themeButton: NSButton!
    private var permissionLabel: NSTextField!

    private var actionsController: ActionsWindowController?

    private let speeds: [(String, Double)] = [
        ("0.5x", 0.5), ("1x", 1.0), ("2x", 2.0), ("4x", 4.0), ("Max", 0.0)
    ]

    convenience init() {
        let width: CGFloat = 380
        let height: CGFloat = 404
        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: width, height: height),
            styleMask: [.titled, .closable, .miniaturizable, .fullSizeContentView],
            backing: .buffered,
            defer: false)
        window.title = "Dolftasker.2"
        window.titleVisibility = .hidden
        window.titlebarAppearsTransparent = true
        window.isMovableByWindowBackground = true
        window.center()
        self.init(window: window)
        buildUI()
        wireEngine()
        updateUI()
    }

    // MARK: - UI

    private func buildUI() {
        guard let window = self.window, let content = window.contentView else { return }
        content.wantsLayer = true
        content.layer?.backgroundColor = Dw.paper.cgColor

        let pad: CGFloat = 10
        let gap: CGFloat = 8
        let width = content.bounds.width
        let inner = width - pad * 2

        // Header. The traffic lights own the left ~70pt, so the mark starts after them.
        let header = FlippedView(frame: NSRect(x: 0, y: 0, width: width, height: 30))
        header.autoresizingMask = [.width]
        let mark = DwMarkView(frame: NSRect(x: 72, y: 7, width: 13, height: 16))
        header.addSubview(mark)
        let title = Dw.label("Dolftasker.2", font: Dw.uiBold, color: Dw.ink)
        title.frame = NSRect(x: 92, y: 6, width: 160, height: 18)
        header.addSubview(title)

        themeButton = NSButton(title: "", target: self, action: #selector(toggleTheme))
        themeButton.isBordered = false
        themeButton.image = NSImage(systemSymbolName: "circle.lefthalf.filled",
                                    accessibilityDescription: "Toggle appearance")
        themeButton.frame = NSRect(x: width - 34, y: 5, width: 22, height: 20)
        themeButton.autoresizingMask = [.minXMargin]
        themeButton.toolTip = "Switch between light and dark"
        header.addSubview(themeButton)
        content.addSubview(header)

        // Placed top-down here, then flipped to AppKit's bottom-up coordinates.
        var y: CGFloat = 38

        // Record / Play — Play is the one signal element on this surface.
        let half = (inner - 6) / 2
        recordButton = Dw.quietButton("● Record", target: self, action: #selector(toggleRecord))
        recordButton.frame = NSRect(x: pad, y: y, width: half, height: 32)
        content.addSubview(recordButton)

        playButton = Dw.signalButton("▶  Play", target: self, action: #selector(togglePlay))
        playButton.frame = NSRect(x: pad + half + 6, y: y, width: inner - half - 6, height: 32)
        content.addSubview(playButton)
        y += 32 + gap

        statusView = DwStatusView(frame: NSRect(x: pad, y: y, width: inner, height: 30))
        content.addSubview(statusView)
        y += 30 + gap

        // Playback
        let playback = Dw.group("Playback")
        playback.frame = NSRect(x: pad, y: y, width: inner, height: 66)
        content.addSubview(playback)
        let playbackBody = FlippedView(frame: playback.contentView?.bounds ?? .zero)
        playbackBody.autoresizingMask = [.width, .height]
        playback.contentView = playbackBody

        let repeatLabel = Dw.label("Repeat", color: Dw.second)
        repeatLabel.frame = NSRect(x: 8, y: 8, width: 48, height: 18)
        playbackBody.addSubview(repeatLabel)

        repeatField = NSTextField(string: "1")
        repeatField.font = Dw.ui
        repeatField.alignment = .center
        repeatField.frame = NSRect(x: 58, y: 6, width: 48, height: 22)
        repeatField.target = self
        repeatField.action = #selector(repeatFieldChanged)
        playbackBody.addSubview(repeatField)

        repeatStepper = NSStepper()
        repeatStepper.minValue = 0
        repeatStepper.maxValue = 100000
        repeatStepper.integerValue = 1
        repeatStepper.valueWraps = false
        repeatStepper.target = self
        repeatStepper.action = #selector(repeatStepperChanged)
        repeatStepper.frame = NSRect(x: 108, y: 6, width: 16, height: 22)
        playbackBody.addSubview(repeatStepper)

        let speedLabel = Dw.label("Speed", color: Dw.second)
        speedLabel.frame = NSRect(x: 138, y: 8, width: 44, height: 18)
        playbackBody.addSubview(speedLabel)

        // A popup is the right control here on macOS — unlike Win32, it themes correctly.
        speedPopup = NSPopUpButton(frame: NSRect(x: 184, y: 5, width: 90, height: 24), pullsDown: false)
        speedPopup.font = Dw.ui
        for (name, _) in speeds { speedPopup.addItem(withTitle: name) }
        speedPopup.selectItem(at: 1)
        playbackBody.addSubview(speedPopup)

        let hint = Dw.label("0 = loop until Esc", font: Dw.uiSmall, color: Dw.muted)
        hint.frame = NSRect(x: 8, y: 32, width: 200, height: 16)
        playbackBody.addSubview(hint)

        y += 66 + gap

        // Capture
        let capture = Dw.group("Capture")
        capture.frame = NSRect(x: pad, y: y, width: inner, height: 88)
        content.addSubview(capture)
        let captureBody = FlippedView(frame: capture.contentView?.bounds ?? .zero)
        captureBody.autoresizingMask = [.width, .height]
        capture.contentView = captureBody

        movesCheck = Dw.checkbox("Record mouse movement", target: self,
                                 action: #selector(captureOptionsChanged), on: true)
        movesCheck.frame = NSRect(x: 8, y: 4, width: inner - 24, height: 20)
        captureBody.addSubview(movesCheck)

        floatCheck = Dw.checkbox("Float above other windows", target: self,
                                 action: #selector(captureOptionsChanged), on: true)
        floatCheck.frame = NSRect(x: 8, y: 26, width: inner - 24, height: 20)
        captureBody.addSubview(floatCheck)

        stopOnMoveCheck = Dw.checkbox("Mouse movement stops playback", target: self,
                                      action: #selector(captureOptionsChanged), on: true)
        stopOnMoveCheck.frame = NSRect(x: 8, y: 48, width: inner - 24, height: 20)
        captureBody.addSubview(stopOnMoveCheck)

        y += 88 + gap

        // Macro
        let macro = Dw.group("Macro")
        macro.frame = NSRect(x: pad, y: y, width: inner, height: 48)
        content.addSubview(macro)
        let macroBody = FlippedView(frame: macro.contentView?.bounds ?? .zero)
        macroBody.autoresizingMask = [.width, .height]
        macro.contentView = macroBody

        let cell = (inner - 16 - 12) / 3
        actionsButton = Dw.quietButton("Actions…", target: self, action: #selector(openActions))
        actionsButton.frame = NSRect(x: 8, y: 4, width: cell, height: 26)
        macroBody.addSubview(actionsButton)

        openButton = Dw.quietButton("Open…", target: self, action: #selector(openFile))
        openButton.frame = NSRect(x: 8 + cell + 6, y: 4, width: cell, height: 26)
        macroBody.addSubview(openButton)

        saveButton = Dw.quietButton("Save…", target: self, action: #selector(saveFile))
        saveButton.frame = NSRect(x: 8 + (cell + 6) * 2, y: 4, width: cell, height: 26)
        macroBody.addSubview(saveButton)

        y += 48 + gap

        let hotkeys = Dw.label("F9 record     F10 play     Esc stop", font: Dw.mono, color: Dw.muted)
        hotkeys.frame = NSRect(x: pad + 1, y: y, width: inner, height: 16)
        content.addSubview(hotkeys)
        y += 18

        permissionLabel = Dw.label("", font: Dw.uiSmall, color: Dw.error)
        permissionLabel.frame = NSRect(x: pad + 1, y: y, width: inner, height: 30)
        permissionLabel.maximumNumberOfLines = 2
        content.addSubview(permissionLabel)

        // Content coordinates are bottom-up; flip everything placed above.
        flipSubviews(of: content)
        applyFloating()
    }

    /// Places subviews using top-down coordinates without wrapping the whole
    /// window in a flipped container.
    private func flipSubviews(of container: NSView) {
        let height = container.bounds.height
        for view in container.subviews {
            var frame = view.frame
            frame.origin.y = height - frame.origin.y - frame.height
            view.frame = frame
        }
    }

    // MARK: - Engine wiring

    private func wireEngine() {
        engine.onHotkeyRecord = { [weak self] in self?.toggleRecord() }
        engine.onHotkeyPlay = { [weak self] in self?.togglePlay() }
        engine.onCancelPlayback = { [weak self] in self?.engine.stopPlayback() }
        engine.onCaptureCountChanged = { [weak self] _ in self?.updateUI() }
        engine.onLoopStarted = { [weak self] pass in
            guard let self = self else { return }
            let total = self.repeatCount == 0 ? "∞" : String(self.repeatCount)
            self.playMeta = "pass \(pass) of \(total)"
            self.updateUI()
        }
        engine.onProgress = { [weak self] p in self?.statusView.progress = p }
        engine.onFinished = { [weak self] in
            self?.statusView.progress = 0
            self?.updateUI()
        }
    }

    func checkPermission() {
        let granted = InputEngine.hasAccessibility(prompting: true)
        if granted {
            permissionLabel.stringValue = ""
            if !engine.startTap() {
                permissionLabel.stringValue = "Could not install the input tap. Try relaunching."
            }
        } else {
            permissionLabel.stringValue =
                "Accessibility access needed: System Settings → Privacy & Security → Accessibility."
        }
    }

    // MARK: - Actions

    @objc private func toggleTheme() {
        Dw.setDark(!Dw.isDark)
        window?.contentView?.layer?.backgroundColor = Dw.paper.cgColor
        window?.contentView?.needsDisplay = true
    }

    @objc private func repeatStepperChanged() {
        repeatField.integerValue = repeatStepper.integerValue
    }

    @objc private func repeatFieldChanged() {
        repeatStepper.integerValue = max(0, repeatField.integerValue)
        repeatField.integerValue = repeatStepper.integerValue
    }

    @objc private func captureOptionsChanged() {
        engine.recordMouseMoves = movesCheck.state == .on
        engine.stopOnMouseMove = stopOnMoveCheck.state == .on
        applyFloating()
    }

    private func applyFloating() {
        window?.level = (floatCheck?.state == .on) ? .floating : .normal
    }

    private var repeatCount: Int { return max(0, repeatField.integerValue) }

    private var speed: Double {
        let index = speedPopup.indexOfSelectedItem
        guard index >= 0 && index < speeds.count else { return 1.0 }
        return speeds[index].1
    }

    @objc private func toggleRecord() {
        if engine.isPlaying { return }

        if engine.isRecording {
            events = engine.stopRecording()
        } else {
            currentURL = nil
            engine.startRecording()
        }
        updateUI()
    }

    @objc private func togglePlay() {
        if engine.isRecording { return }

        if engine.isPlaying {
            engine.stopPlayback()
            return
        }
        guard !events.isEmpty else { return }
        engine.play(events, repeatCount: repeatCount, speed: speed)
        updateUI()
    }

    @objc private func openActions() {
        guard !engine.isRecording, !engine.isPlaying, !events.isEmpty else { return }
        let controller = ActionsWindowController(events: events, repeatCount: repeatCount)
        controller.onApply = { [weak self] edited in
            self?.events = edited
            self?.updateUI()
        }
        actionsController = controller
        controller.showWindow(self)
    }

    @objc private func openFile() {
        let panel = NSOpenPanel()
        panel.allowedFileTypes = ["dolfrec", "json"]
        panel.allowsMultipleSelection = false
        panel.begin { [weak self] response in
            guard let self = self, response == .OK, let url = panel.url else { return }
            do {
                self.events = try MacroFile.load(from: url)
                self.currentURL = url
            } catch {
                self.presentError(error)
            }
            self.updateUI()
        }
    }

    @objc private func saveFile() {
        guard !events.isEmpty else { return }
        let panel = NSSavePanel()
        panel.allowedFileTypes = ["dolfrec"]
        panel.nameFieldStringValue = currentURL?.lastPathComponent ?? "macro.dolfrec"
        panel.begin { [weak self] response in
            guard let self = self, response == .OK, let url = panel.url else { return }
            do {
                try MacroFile.save(self.events, to: url)
                self.currentURL = url
            } catch {
                self.presentError(error)
            }
            self.updateUI()
        }
    }

    private func presentError(_ error: Error) {
        guard let window = self.window else { return }
        NSAlert(error: error).beginSheetModal(for: window, completionHandler: nil)
    }

    // MARK: - State

    private func updateUI() {
        let recording = engine.isRecording
        let playing = engine.isPlaying
        let idle = !recording && !playing

        recordButton.title = recording ? "● Stop recording" : "● Record"
        recordButton.isEnabled = !playing
        setBlinking(recording)

        playButton.title = playing ? "■  Stop" : "▶  Play"
        playButton.isEnabled = !recording && !events.isEmpty

        openButton.isEnabled = idle
        saveButton.isEnabled = idle && !events.isEmpty
        actionsButton.isEnabled = idle && !events.isEmpty
        repeatField.isEnabled = !playing
        repeatStepper.isEnabled = !playing
        speedPopup.isEnabled = !playing

        if recording {
            let count = engine.capturedEvents.count
            statusView.set(state: "RECORDING", meta: "\(count) steps captured",
                           progress: 1, accent: Dw.error)
        } else if playing {
            statusView.set(state: "PLAYING BACK", meta: playMeta, progress: nil, accent: Dw.signal)
        } else if events.isEmpty {
            statusView.set(state: "READY", meta: "no macro loaded", progress: 0, accent: Dw.signal)
        } else {
            let seconds = Double(MacroFile.totalMilliseconds(events)) / 1000.0
            let name = currentURL?.deletingPathExtension().lastPathComponent.uppercased() ?? "READY"
            statusView.set(state: name,
                           meta: String(format: "%d steps · %.2f s", events.count, seconds),
                           progress: 0, accent: Dw.signal)
        }
    }

    private func setBlinking(_ on: Bool) {
        if on {
            if blinkTimer == nil {
                blinkTimer = Timer.scheduledTimer(withTimeInterval: 0.5, repeats: true) { [weak self] _ in
                    guard let self = self else { return }
                    self.blinkOn.toggle()
                    self.recordButton.title = self.blinkOn ? "● Stop recording" : "○ Stop recording"
                }
            }
        } else {
            blinkTimer?.invalidate()
            blinkTimer = nil
            blinkOn = false
        }
    }
}

/// Top-left origin, so layout maths matches the Windows build.
final class FlippedView: NSView {
    override var isFlipped: Bool { return true }
}
