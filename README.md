# DolfTask

A small mouse & keyboard macro recorder for Windows, in the spirit of TinyTask.
Records what you do, replays it exactly, saves it to a file.

Styled to the **DOLFWA design system**: three colours, flat surfaces, tight
groups, one signal element per window.

The whole thing is a single **45 KB `.exe`** with no dependencies — it uses only
Win32 APIs and the .NET Framework that's already part of Windows.

## Design

The design's colours, spacing and layout applied to **stock Windows controls** —
`Button`, `Label`, `GroupBox`, `CheckBox`, `NumericUpDown`, `ComboBox`,
`DataGridView`. Nothing is a picture and nothing is custom-painted, so all text
is rendered by GDI and stays sharp at any DPI.

- **30px title bar.** The window is borderless with its own chrome: the DOLFWA
  mark (drawn geometry, not an image), the window name, a light/dark toggle, and
  flat minimise / close glyphs.
- **Light and dark**, both from the design's own palettes. The half-circle glyph
  in the title bar switches between them instantly; the choice is remembered in
  `HKCU\Software\DOLFWA\DolfTask`. On first run DolfTask follows the Windows
  app-theme setting.
- **One signal element per window.** In the main window that's **Play**, in
  signal blue; Record stays a quiet white control and blinks its dot error-red
  only while recording — the sanctioned exception, because recording is a state
  you must not miss. In the Actions window the signal is the `FIRST` / `LAST`
  markers, which is why **Apply** is ink rather than blue.
- **26px controls** in tight group boxes — Playback, Capture, Macro — with
  uppercase legends.
- **A bordered status strip**: run state at the left, detail at the right, and a
  2px progress rule beneath that fills as playback advances, red while
  recording and signal while playing.
- Quiet controls wash 10% signal on hover; signal fills darken on hover and
  press; disabled controls mute rather than change colour.

**Typography.** Segoe UI for UI text, Consolas for the status strip, grid cells
and hotkey line. The design specifies Sora and Space Mono, which aren't
installed on Windows by default; the substitutes keep every glyph hinted by GDI,
which is what keeps small text legible.

## Build

```bash
build.bat
```

Produces `bin\DolfTask.exe`. No SDK or downloads needed — the script uses the C#
compiler shipped with Windows (`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`).

## Use

| Action | Hotkey | Button |
|---|---|---|
| Start / stop recording | `F9` | ● Record |
| Start / stop playback | `F10` | ▶ Play |
| Abort playback | `Esc` | ■ Stop |

The hotkeys are global, so they work while another app has focus — that's the
normal way to use it. Recording captures mouse movement, all five mouse buttons,
both scroll axes, and every keystroke with its exact timing.

Options:

- **Repeat** — number of passes; `0` loops forever until you press `Esc`.
- **Speed** — click to cycle `0.5x` → `1x` → `2x` → `4x` → `Max` (no delays).
- **Record mouse movement** — off records only clicks (each click still lands at
  the right spot), which makes for much smaller, more robust macros.
- **Record window switches** — captures an **Activate Window** step whenever you
  switch apps while recording. See below.
- **Always on top** — keeps the window visible over the app you're automating.
- **Game mode (scan codes)** — sends keys as hardware scan codes instead of
  virtual key codes. Many games and remote-desktop clients ignore normal
  injected keys; turn this on if keystrokes don't register.

**Open…** / **Save…** read and write `.rec` files, so you can keep a library of
macros and reuse them later.

## Switching windows (games, and anything else)

Clicking an unfocused window makes that click do two jobs: it changes the
foreground window *and* it reaches the app. Games are the worst case — the click
that brings the game forward is swallowed by the focus change, so on playback the
macro is one click short and everything after it lands in the wrong place.

DolfTasker records the focus change as its own step, so the click no longer has
to carry it:

| # | Action | Window | Wait ms |
|---|---|---|---|
| 1 | Activate Window | RobloxPlayerBeta | 0 |
| 2 | Mouse Move | | 40 |
| 3 | Left Down | | 50 |

On playback the **Activate Window** step brings the target forward and waits
~120 ms for it to actually get focus, so the click that follows is a real
in-game click.

The target is stored as the **executable name**, not a window handle or caption —
handles change every session and a game's title bar changes with the place you're
in, but `RobloxPlayerBeta` stays put. You can retype the target in the Actions
window, or add an **Activate Window** step by hand to switch apps mid-macro.

If the window isn't open at playback time the step is skipped and the rest of the
macro still runs.

## Editing a macro — the Actions window

**Actions…** opens the recorded steps in a spreadsheet-style list, so the main
window stays small and you only see the detail when you ask for it.

| # | Action | X | Y | Key | Wheel | Window | Wait ms |
|---|---|---|---|---|---|---|---|
| 1 FIRST | Activate Window | — | — | — | — | RobloxPlayerBeta | 0 |
| 2 | Mouse Move | 640 | 480 | — | — | — | 40 |
| 3 | Left Down | — | — | — | — | — | 50 |
| 4 LAST | Key Down | — | — | A | — | — | 120 |

Every cell is editable in place. Cells that don't apply to a row read `—` and
can't be edited, so a key row can't be given coordinates by accident.

The step the macro **starts** on is marked `FIRST` and the one it **ends** on is
marked `LAST`, both in signal blue — the only blue in the window, so the run
boundaries stay findable once the list scrolls. They follow the rows as you
insert, delete or reorder, and a single-step macro reads `ONLY`.

- **Wait (ms)** is the pause *before* that action — this is where you slow a
  macro down, speed one step up, or turn a fumbled 3-second pause into 200 ms.
- **Action** is a drop-down, so turning step 3 from a left click into a right
  click (or a middle click, or an X-button click) is one change. You can even
  retype a mouse step as a keystroke.
- **X / Y** reposition a click; **Key** picks any virtual key; **Wheel** sets the
  scroll amount (120 = one notch up, -120 = one down).
- **Insert** / **Delete** / **▲** / **▼** add, remove and reorder steps.
- **Apply** commits the edits, **Cancel** throws them away. The running total at
  the bottom shows how long the macro takes.

Changing a key re-derives its scan code automatically, and keys you *didn't*
touch keep the exact scan code that was recorded — so editing one step never
degrades the rest of the macro.

## How it works

- Recording uses low-level `WH_MOUSE_LL` / `WH_KEYBOARD_LL` hooks, storing each
  event with the millisecond gap since the previous one.
- Playback uses `SendInput` on a background thread with a hybrid
  sleep/spin timer, so timing stays accurate instead of being rounded up to
  Windows' ~15 ms scheduler tick.
- Mouse positions are stored as virtual-desktop pixels and replayed as absolute
  normalized coordinates, so multi-monitor setups work correctly.
- The app is per-monitor DPI aware, so coordinates are right on mixed-DPI setups.
- Injected events are tagged and ignored by the hooks, so playback can never
  record itself into a feedback loop.
- If you abort mid-macro, any keys the macro was holding down get released, so
  you're never left with a stuck `Ctrl` or `Alt`.

## Notes

- Playback starts after a ~250 ms grace period so the click or keypress that
  started it doesn't get mixed into the macro.
- Mouse movement made *after* you stop recording is trimmed, so the trip back to
  the Stop button isn't replayed.
- To automate a program running **as administrator**, run `DolfTask.exe` as
  administrator too — Windows blocks input injection from a lower integrity
  level.
- Some anti-cheat systems block all synthetic input regardless; that's enforced
  at the driver level and no user-mode recorder can work around it.

## Files

- `src/MacroRecorder.cs` — the entire application
- `src/app.manifest` — visual styles + DPI awareness
- `build.bat` — compiles it
