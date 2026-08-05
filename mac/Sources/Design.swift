// DOLFWA styling for macOS.
//
// Colours are dynamic NSColors, so switching NSApp.appearance retints the branded
// views and every stock AppKit control at once — the Windows build needed a manual
// walk of the control tree to do the same thing.

import AppKit

extension NSColor {
    convenience init(hex: UInt32) {
        self.init(srgbRed: CGFloat((hex >> 16) & 0xFF) / 255.0,
                  green: CGFloat((hex >> 8) & 0xFF) / 255.0,
                  blue: CGFloat(hex & 0xFF) / 255.0,
                  alpha: 1.0)
    }

    /// Picks a colour from the appearance in effect when the view draws.
    static func dolfwa(light: UInt32, dark: UInt32) -> NSColor {
        return NSColor(name: nil) { appearance in
            let isDark = appearance.bestMatch(from: [.aqua, .darkAqua]) == .darkAqua
            return NSColor(hex: isDark ? dark : light)
        }
    }
}

enum Dw {

    // Signal and error are fixed in both themes — the system's closed set.
    static let signal = NSColor(hex: 0x2B5CE6)
    static let signalHover = NSColor(hex: 0x2450C9)
    static let signalPress = NSColor(hex: 0x1F45B4)
    static let error = NSColor(hex: 0xC4453A)

    // Role colours. In dark, "ink" is a light text colour and "paper" a dark surface.
    static let paper = NSColor.dolfwa(light: 0xF2F4F1, dark: 0x192421)
    static let surface = NSColor.dolfwa(light: 0xFFFFFF, dark: 0x222C29)
    static let chrome = NSColor.dolfwa(light: 0xFFFFFF, dark: 0x283330)
    static let hairline = NSColor.dolfwa(light: 0xDDE0DC, dark: 0x3A4340)
    static let lineSoft = NSColor.dolfwa(light: 0xE4E7E3, dark: 0x333C39)
    static let ink = NSColor.dolfwa(light: 0x0F1A17, dark: 0xF2F4F1)
    static let second = NSColor.dolfwa(light: 0x4A4E4B, dark: 0xB5BAB7)
    static let muted = NSColor.dolfwa(light: 0x6B6F6C, dark: 0x7B827F)
    static let well = NSColor.dolfwa(light: 0xE4E7E3, dark: 0x283330)
    static let onSignal = NSColor.white

    // MARK: Type — the macOS counterparts of Segoe UI and Consolas.

    static let ui = NSFont.systemFont(ofSize: 12)
    static let uiBold = NSFont.systemFont(ofSize: 12, weight: .semibold)
    static let uiSmall = NSFont.systemFont(ofSize: 11)
    static let mono = NSFont.monospacedSystemFont(ofSize: 11, weight: .regular)
    static let monoBold = NSFont.monospacedSystemFont(ofSize: 11, weight: .semibold)
    static let legend = NSFont.systemFont(ofSize: 10, weight: .semibold)

    // MARK: Theme

    static let darkDefaultsKey = "DolfwaDarkAppearance"

    static var isDark: Bool {
        return NSApp.effectiveAppearance.bestMatch(from: [.aqua, .darkAqua]) == .darkAqua
    }

    static func applyStoredAppearance() {
        // Nothing stored on first run means follow the system.
        if let stored = UserDefaults.standard.object(forKey: darkDefaultsKey) as? Bool {
            NSApp.appearance = NSAppearance(named: stored ? .darkAqua : .aqua)
        } else {
            NSApp.appearance = nil
        }
    }

    static func setDark(_ dark: Bool) {
        UserDefaults.standard.set(dark, forKey: darkDefaultsKey)
        NSApp.appearance = NSAppearance(named: dark ? .darkAqua : .aqua)
    }

    // MARK: Builders

    static func label(_ text: String,
                      font: NSFont = Dw.ui,
                      color: NSColor = Dw.ink,
                      align: NSTextAlignment = .left) -> NSTextField {
        let field = NSTextField(labelWithString: text)
        field.font = font
        field.textColor = color
        field.alignment = align
        field.lineBreakMode = .byTruncatingTail
        return field
    }

    /// Quiet control: surface with a hairline border.
    static func quietButton(_ title: String, target: AnyObject?, action: Selector) -> DwButton {
        let b = DwButton(title: title, target: target, action: action)
        b.variant = .quiet
        return b
    }

    /// The one signal element on a surface.
    static func signalButton(_ title: String, target: AnyObject?, action: Selector) -> DwButton {
        let b = DwButton(title: title, target: target, action: action)
        b.variant = .signal
        return b
    }

    /// Ink fill: confirms without competing with the signal.
    static func inkButton(_ title: String, target: AnyObject?, action: Selector) -> DwButton {
        let b = DwButton(title: title, target: target, action: action)
        b.variant = .ink
        return b
    }

    static func checkbox(_ title: String, target: AnyObject?, action: Selector, on: Bool) -> NSButton {
        let b = NSButton(checkboxWithTitle: title, target: target, action: action)
        b.font = Dw.ui
        b.state = on ? .on : .off
        b.contentTintColor = nil
        return b
    }

    /// A hairline box with an uppercase legend.
    static func group(_ legend: String) -> NSBox {
        let box = NSBox()
        box.title = legend.uppercased()
        box.titleFont = Dw.legend
        box.titlePosition = .atTop
        // borderColor / fillColor / cornerRadius are only honoured by .custom;
        // with .primary the box keeps the system bezel and ignores them.
        box.boxType = .custom
        box.borderColor = Dw.hairline
        box.borderWidth = 1
        box.cornerRadius = 4
        box.fillColor = .clear
        box.contentViewMargins = .zero
        return box
    }
}

// MARK: - Buttons

/// AppKit's bezel can't take arbitrary fills, so the button draws its own 4px
/// rounded rect. The title is still drawn by AppKit, so text stays sharp.
final class DwButton: NSButton {

    enum Variant { case quiet, signal, ink }

    var variant: Variant = .quiet { didSet { needsDisplay = true } }
    private var hovering = false

    init(title: String, target: AnyObject?, action: Selector) {
        super.init(frame: .zero)
        self.title = title
        self.target = target
        self.action = action
        isBordered = false
        wantsLayer = true
        font = Dw.ui
        layer?.cornerRadius = 4
        layer?.borderWidth = 1
    }

    required init?(coder: NSCoder) { fatalError("not used") }

    override func updateTrackingAreas() {
        super.updateTrackingAreas()
        for area in trackingAreas { removeTrackingArea(area) }
        addTrackingArea(NSTrackingArea(
            rect: bounds,
            options: [.mouseEnteredAndExited, .activeInActiveApp, .inVisibleRect],
            owner: self, userInfo: nil))
    }

    override func mouseEntered(with event: NSEvent) { hovering = true; needsDisplay = true }
    override func mouseExited(with event: NSEvent) { hovering = false; needsDisplay = true }

    override func viewDidChangeEffectiveAppearance() {
        super.viewDidChangeEffectiveAppearance()
        needsDisplay = true
    }

    override func draw(_ dirtyRect: NSRect) {
        // Resolve dynamic colours against this view's appearance.
        effectiveAppearance.performAsCurrentDrawingAppearance {
            let fill: NSColor
            let border: NSColor
            let text: NSColor

            switch variant {
            case .signal:
                fill = isEnabled ? (hovering ? Dw.signalHover : Dw.signal)
                                 : Dw.signal.withAlphaComponent(0.4)
                border = fill
                text = isEnabled ? Dw.onSignal : Dw.onSignal.withAlphaComponent(0.6)
            case .ink:
                fill = isEnabled ? (hovering ? Dw.second : Dw.ink) : Dw.ink.withAlphaComponent(0.4)
                border = fill
                text = Dw.paper
            case .quiet:
                fill = hovering && isEnabled ? Dw.signal.withAlphaComponent(0.10) : Dw.surface
                border = hovering && isEnabled ? Dw.signal : Dw.hairline
                text = isEnabled ? (hovering ? Dw.signal : Dw.ink) : Dw.muted
            }

            layer?.backgroundColor = fill.cgColor
            layer?.borderColor = border.cgColor

            let style = NSMutableParagraphStyle()
            style.alignment = alignment
            let attributed = NSAttributedString(string: title, attributes: [
                .font: font ?? Dw.ui,
                .foregroundColor: text,
                .paragraphStyle: style
            ])
            let size = attributed.size()
            let rect = NSRect(x: 0,
                              y: (bounds.height - size.height) / 2,
                              width: bounds.width,
                              height: size.height)
            attributed.draw(in: rect)
        }
    }
}

// MARK: - Branded views

/// The DOLFWA mark: two nested rounded rectangles, the counter knocked out in
/// the surface colour. Geometry, never an image.
final class DwMarkView: NSView {
    override var isFlipped: Bool { return true }

    override func draw(_ dirtyRect: NSRect) {
        effectiveAppearance.performAsCurrentDrawingAppearance {
            let s = bounds.height / 16.0
            let outer = NSBezierPath(roundedRect: NSRect(x: 0, y: 0, width: 13 * s, height: 16 * s),
                                     xRadius: 3 * s, yRadius: 3 * s)
            Dw.ink.setFill()
            outer.fill()

            let inner = NSBezierPath(roundedRect: NSRect(x: 3.9 * s, y: 4 * s,
                                                         width: 7.4 * s, height: 8 * s),
                                     xRadius: 2 * s, yRadius: 2 * s)
            Dw.chrome.setFill()
            inner.fill()
        }
    }
}

/// The recessed status well: state at the left, detail at the right, and a 2px
/// progress rule beneath that fills as playback advances.
final class DwStatusView: NSView {

    private let stateLabel = Dw.label("READY", font: Dw.monoBold, color: Dw.ink)
    private let metaLabel = Dw.label("", font: Dw.mono, color: Dw.muted, align: .right)

    var progress: Double = 0 { didSet { needsDisplay = true } }
    var accent: NSColor = Dw.signal { didSet { needsDisplay = true } }

    override var isFlipped: Bool { return true }

    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        wantsLayer = true
        addSubview(stateLabel)
        addSubview(metaLabel)
    }

    required init?(coder: NSCoder) { fatalError("not used") }

    func set(state: String, meta: String, progress: Double?, accent: NSColor) {
        stateLabel.stringValue = state
        stateLabel.textColor = accent == Dw.error ? Dw.error : Dw.ink
        metaLabel.stringValue = meta
        self.accent = accent
        if let p = progress { self.progress = p }
        needsDisplay = true
    }

    override func layout() {
        super.layout()
        let inset: CGFloat = 8
        stateLabel.frame = NSRect(x: inset, y: 4, width: bounds.width / 2 - inset, height: 16)
        metaLabel.frame = NSRect(x: bounds.width / 2, y: 4,
                                 width: bounds.width / 2 - inset, height: 16)
    }

    override func draw(_ dirtyRect: NSRect) {
        effectiveAppearance.performAsCurrentDrawingAppearance {
            let box = NSBezierPath(roundedRect: bounds.insetBy(dx: 0.5, dy: 0.5),
                                   xRadius: 4, yRadius: 4)
            Dw.well.setFill()
            box.fill()
            Dw.hairline.setStroke()
            box.stroke()

            let trackHeight: CGFloat = 2
            let trackRect = NSRect(x: 1, y: bounds.height - trackHeight - 1,
                                   width: bounds.width - 2, height: trackHeight)
            Dw.hairline.setFill()
            trackRect.fill()

            if progress > 0 {
                let filled = NSRect(x: trackRect.minX, y: trackRect.minY,
                                    width: trackRect.width * CGFloat(min(1, max(0, progress))),
                                    height: trackHeight)
                accent.setFill()
                filled.fill()
            }
        }
    }
}
