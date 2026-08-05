// The macro model and its file format.
//
// This is deliberately NOT the Windows .rec format. Windows stores virtual-key
// codes and scan codes; macOS uses its own virtual key codes, so a Windows
// recording replayed here would press the wrong keys. The `format` field makes
// that mismatch a clear error instead of silent nonsense.

import Foundation

enum EvKind: Int, Codable {
    case mouseMove = 0
    case mouseButton = 1
    case scroll = 2
    case key = 3
    case flags = 4      // modifier press/release (macOS reports these separately)
}

struct MacroEvent: Codable {
    var kind: EvKind
    var delay: Int          // milliseconds to wait before this event

    var x: Double           // global screen position, top-left origin
    var y: Double

    var button: Int         // CGMouseButton raw value
    var isDown: Bool

    var keyCode: Int
    var flags: UInt64       // CGEventFlags raw value

    var scrollY: Int
    var scrollX: Int

    init(kind: EvKind,
         delay: Int = 0,
         x: Double = 0, y: Double = 0,
         button: Int = 0, isDown: Bool = false,
         keyCode: Int = 0, flags: UInt64 = 0,
         scrollY: Int = 0, scrollX: Int = 0) {
        self.kind = kind
        self.delay = delay
        self.x = x
        self.y = y
        self.button = button
        self.isDown = isDown
        self.keyCode = keyCode
        self.flags = flags
        self.scrollY = scrollY
        self.scrollX = scrollX
    }
}

// MARK: - Naming, for the Actions editor

enum ActionName {
    static let move = "Mouse Move"
    static let scroll = "Scroll"

    static let all: [String] = [
        move,
        "Left Down", "Left Up",
        "Right Down", "Right Up",
        "Middle Down", "Middle Up",
        scroll,
        "Key Down", "Key Up",
        "Modifiers"
    ]

    static func of(_ e: MacroEvent) -> String {
        switch e.kind {
        case .mouseMove: return move
        case .scroll: return scroll
        case .flags: return "Modifiers"
        case .key: return e.isDown ? "Key Down" : "Key Up"
        case .mouseButton:
            let side: String
            switch e.button {
            case 1: side = "Right"
            case 0: side = "Left"
            default: side = "Middle"
            }
            return side + (e.isDown ? " Down" : " Up")
        }
    }

    /// Rewrites kind/button/isDown to match a name chosen in the editor.
    static func apply(_ name: String, to e: inout MacroEvent) {
        switch name {
        case move: e.kind = .mouseMove
        case scroll: e.kind = .scroll
        case "Modifiers": e.kind = .flags
        case "Key Down": e.kind = .key; e.isDown = true
        case "Key Up": e.kind = .key; e.isDown = false
        default:
            e.kind = .mouseButton
            e.isDown = name.hasSuffix("Down")
            if name.hasPrefix("Left") { e.button = 0 }
            else if name.hasPrefix("Right") { e.button = 1 }
            else { e.button = 2 }
        }
    }

    static func isMove(_ n: String) -> Bool { return n == move }
    static func isScroll(_ n: String) -> Bool { return n == scroll }
    static func isKey(_ n: String) -> Bool { return n == "Key Down" || n == "Key Up" || n == "Modifiers" }
}

// MARK: - macOS virtual key codes

enum KeyNames {
    /// Only the keys worth naming; anything else shows as "Key 123".
    static let table: [Int: String] = [
        0: "A", 1: "S", 2: "D", 3: "F", 4: "H", 5: "G", 6: "Z", 7: "X", 8: "C", 9: "V",
        11: "B", 12: "Q", 13: "W", 14: "E", 15: "R", 16: "Y", 17: "T",
        31: "O", 32: "U", 34: "I", 35: "P", 37: "L", 38: "J", 40: "K", 45: "N", 46: "M",
        18: "1", 19: "2", 20: "3", 21: "4", 22: "6", 23: "5", 25: "9", 26: "7", 28: "8", 29: "0",
        24: "Equals", 27: "Minus", 30: "Right Bracket", 33: "Left Bracket",
        39: "Quote", 41: "Semicolon", 42: "Backslash", 43: "Comma",
        44: "Slash", 47: "Period", 50: "Grave",
        36: "Return", 48: "Tab", 49: "Space", 51: "Delete", 53: "Escape",
        55: "Command", 56: "Shift", 57: "Caps Lock", 58: "Option", 59: "Control",
        60: "Right Shift", 61: "Right Option", 62: "Right Control", 63: "Function",
        65: "Keypad Decimal", 67: "Keypad Multiply", 69: "Keypad Plus", 71: "Keypad Clear",
        75: "Keypad Divide", 76: "Keypad Enter", 78: "Keypad Minus", 81: "Keypad Equals",
        82: "Keypad 0", 83: "Keypad 1", 84: "Keypad 2", 85: "Keypad 3", 86: "Keypad 4",
        87: "Keypad 5", 88: "Keypad 6", 89: "Keypad 7", 91: "Keypad 8", 92: "Keypad 9",
        96: "F5", 97: "F6", 98: "F7", 99: "F3", 100: "F8", 101: "F9", 103: "F11",
        109: "F10", 111: "F12", 105: "F13", 107: "F14", 113: "F15",
        114: "Help", 115: "Home", 116: "Page Up", 117: "Forward Delete",
        118: "F4", 119: "End", 120: "F2", 121: "Page Down", 122: "F1",
        123: "Left Arrow", 124: "Right Arrow", 125: "Down Arrow", 126: "Up Arrow"
    ]

    static let escape = 53
    static let f9 = 101
    static let f10 = 109

    static func name(_ code: Int) -> String {
        if let n = table[code] { return n }
        return "Key \(code)"
    }

    /// Sorted list for the editor's popup, with the raw code carried alongside.
    static func sortedList() -> [(code: Int, name: String)] {
        var list: [(code: Int, name: String)] = []
        for (code, name) in table { list.append((code, name)) }
        list.sort { $0.name.localizedCaseInsensitiveCompare($1.name) == .orderedAscending }
        return list
    }
}

// MARK: - File

struct MacroDocument: Codable {
    static let currentFormat = "dolftasker-mac-1"

    var format: String
    var events: [MacroEvent]
}

enum MacroFile {
    static func save(_ events: [MacroEvent], to url: URL) throws {
        let doc = MacroDocument(format: MacroDocument.currentFormat, events: events)
        let encoder = JSONEncoder()
        encoder.outputFormatting = .prettyPrinted
        try encoder.encode(doc).write(to: url)
    }

    static func load(from url: URL) throws -> [MacroEvent] {
        let data = try Data(contentsOf: url)
        let doc: MacroDocument
        do {
            doc = try JSONDecoder().decode(MacroDocument.self, from: data)
        } catch {
            throw NSError(domain: "Dolftasker", code: 1, userInfo: [
                NSLocalizedDescriptionKey: "Not a Dolftasker.2 recording.",
                NSLocalizedRecoverySuggestionErrorKey:
                    "Recordings made on Windows use a different key-code format and can't be replayed on macOS."
            ])
        }
        guard doc.format == MacroDocument.currentFormat else {
            throw NSError(domain: "Dolftasker", code: 2, userInfo: [
                NSLocalizedDescriptionKey: "Unsupported recording format \"\(doc.format)\"."
            ])
        }
        return doc.events
    }

    static func totalMilliseconds(_ events: [MacroEvent]) -> Int {
        var total = 0
        for e in events { total += e.delay }
        return total
    }
}
