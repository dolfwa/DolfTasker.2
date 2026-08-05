# Dolftasker.2 for macOS

The macOS build of Dolftasker.2 — a mouse and keyboard macro recorder.

Swift + AppKit, compiled by the Swift compiler that ships with the Xcode Command
Line Tools. No Xcode project, no package manager, no dependencies.

## Build

```bash
./build.sh
open "build/Dolftasker.2.app"
```

If `swiftc` is missing, install the command line tools once:

```bash
xcode-select --install
```

## Accessibility permission

macOS will not let any app observe or synthesize input without explicit
permission. On first launch you'll get a prompt; if you miss it, grant it under:

**System Settings → Privacy & Security → Accessibility** → enable
**Dolftasker.2**.

Nothing works until this is granted — recording captures nothing and playback
posts nothing. The main window shows a red line when the permission is missing.

`build.sh` ad-hoc signs the app, which keeps the permission grant stable across
rebuilds. Without that signature macOS treats each rebuild as a new app and you'd
re-approve it every time.

## Use

| Action | Hotkey |
|---|---|
| Start / stop recording | `F9` |
| Start / stop playback | `F10` |
| Abort playback | `Esc`, or just move the mouse |

The hotkeys are global, and `F9` / `F10` are swallowed so they don't reach the
app underneath.

Options mirror the Windows build: **Repeat** (`0` loops until stopped),
**Speed** (`0.5x`–`4x`, or `Max` for no delays), **Record mouse movement**,
**Float above other windows**, and **Mouse movement stops playback**.

**Actions…** opens the editable step list — action type, coordinates, key,
scroll amount and the wait before each step, with `FIRST` / `LAST` marking the
run boundaries.

## How it differs from the Windows build

The engine is a genuine rewrite; only the design and the interaction model carry
over.

| | Windows | macOS |
|---|---|---|
| Capture | `SetWindowsHookEx` low-level hooks | Quartz event tap (`CGEvent.tapCreate`) |
| Playback | `SendInput` | `CGEvent.post` |
| Self-event tagging | `dwExtraInfo` | `.eventSourceUserData` |
| Hotkeys | `RegisterHotKey` | consumed in the event tap |
| Key identity | virtual key + scan code | macOS virtual key code |
| Modifiers | ordinary key down/up | separate `flagsChanged` events |
| Theming | manual walk of the control tree | dynamic `NSColor` + `NSApp.appearance` |

Consequences worth knowing:

- **Recordings are not portable between the two builds.** Key codes mean
  different things on each platform, so a Windows `.rec` replayed here would
  press the wrong keys. macOS files use the `.dolfrec` extension and carry a
  format tag; loading a foreign file gives a clear error instead of garbage.
- **Modifier keys** (⌘ ⇧ ⌥ ⌃) arrive as `flagsChanged` rather than key presses,
  so they're recorded as their own step type and replayed by setting the event's
  flag mask.
- **Drags replay as drags.** Held buttons are tracked during playback so mouse
  movement is posted as `leftMouseDragged` rather than `mouseMoved`, which is
  what apps actually respond to.
- **Speed is a popup, not a cycling button.** The cycling button on Windows
  worked around a Win32 combo box that ignores theming; `NSPopUpButton` has no
  such problem, so the native control is the right choice here.
- **Secure input.** While a password field has secure input enabled, macOS
  blocks keystroke capture entirely. That's an OS guarantee no app can work
  around.
