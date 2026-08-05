// Dolftasker.2 - a small mouse & keyboard macro recorder for Windows.
// Build: build.bat  (uses the .NET Framework 4 compiler that ships with Windows)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace DolfTask
{
    #region Win32

    internal static class Native
    {
        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;

        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_RBUTTONUP = 0x0205;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MBUTTONUP = 0x0208;
        public const int WM_XBUTTONDOWN = 0x020B;
        public const int WM_XBUTTONUP = 0x020C;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_MOUSEHWHEEL = 0x020E;

        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_HOTKEY = 0x0312;

        public const uint LLMHF_INJECTED = 0x00000001;
        public const uint LLKHF_EXTENDED = 0x00000001;
        public const uint LLKHF_INJECTED = 0x00000010;

        public const uint INPUT_MOUSE = 0;
        public const uint INPUT_KEYBOARD = 1;

        public const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        public const uint MOUSEEVENTF_XDOWN = 0x0080;
        public const uint MOUSEEVENTF_XUP = 0x0100;
        public const uint MOUSEEVENTF_WHEEL = 0x0800;
        public const uint MOUSEEVENTF_HWHEEL = 0x1000;
        public const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
        public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const uint KEYEVENTF_SCANCODE = 0x0008;

        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;

        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;

        // Tag placed on every event we synthesize, so the hooks can ignore them.
        public static readonly IntPtr SelfTag = new IntPtr(0x444F4C46); // 'DOLF'

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public INPUTUNION u;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        public const uint MAPVK_VK_TO_VSC = 0;

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetProcessDPIAware();

        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint uPeriod);
    }

    #endregion

    #region Macro model

    internal enum EvKind : byte
    {
        MouseMove = 0,   // a = x, b = y (virtual-desktop pixels)
        MouseButton = 1, // a = MOUSEEVENTF_* flag, b = mouseData (X button index)
        MouseWheel = 2,  // a = MOUSEEVENTF_WHEEL/HWHEEL, b = signed delta
        KeyDown = 3,     // a = vk, b = scan code, c = 1 if extended key
        KeyUp = 4
    }

    internal struct MacroEvent
    {
        public EvKind Kind;
        public int Delay; // milliseconds to wait before this event
        public int A, B, C;

        public MacroEvent(EvKind kind, int delay, int a, int b, int c)
        {
            Kind = kind; Delay = delay; A = a; B = b; C = c;
        }
    }

    internal static class MacroFile
    {
        private const uint Magic = 0x314B5444; // "DTK1"

        public static void Save(string path, List<MacroEvent> events)
        {
            using (var fs = File.Create(path))
            using (var w = new BinaryWriter(fs))
            {
                w.Write(Magic);
                w.Write(events.Count);
                foreach (var e in events)
                {
                    w.Write((byte)e.Kind);
                    w.Write(e.Delay);
                    w.Write(e.A);
                    w.Write(e.B);
                    w.Write(e.C);
                }
            }
        }

        public static List<MacroEvent> Load(string path)
        {
            using (var fs = File.OpenRead(path))
            using (var r = new BinaryReader(fs))
            {
                if (r.ReadUInt32() != Magic)
                    throw new InvalidDataException("Not a Dolftasker.2 recording (.rec) file.");
                int count = r.ReadInt32();
                if (count < 0) throw new InvalidDataException("Corrupt recording file.");
                var list = new List<MacroEvent>(count);
                for (int i = 0; i < count; i++)
                {
                    var kind = (EvKind)r.ReadByte();
                    int delay = r.ReadInt32();
                    int a = r.ReadInt32();
                    int b = r.ReadInt32();
                    int c = r.ReadInt32();
                    list.Add(new MacroEvent(kind, delay, a, b, c));
                }
                return list;
            }
        }
    }

    #endregion

    #region Playback

    internal sealed class Player
    {
        private readonly List<MacroEvent> _events;
        private readonly int _repeat;      // 0 = until stopped
        private readonly double _speed;    // 0 = no delays at all
        private readonly bool _useScanCodes;
        private volatile bool _stop;
        private Thread _thread;
        private readonly HashSet<int> _keysDown = new HashSet<int>();

        public event Action<int> LoopStarted;  // 1-based iteration
        public event Action<double> Progressed; // 0..1 through the current pass
        public event Action Finished;

        public Player(List<MacroEvent> events, int repeat, double speed, bool useScanCodes)
        {
            _events = events;
            _repeat = repeat;
            _speed = speed;
            _useScanCodes = useScanCodes;
        }

        public bool IsRunning { get { return _thread != null && _thread.IsAlive; } }

        public void Start()
        {
            _stop = false;
            _thread = new Thread(Run);
            _thread.IsBackground = true;
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        public void Stop() { _stop = true; }

        private void Run()
        {
            Native.timeBeginPeriod(1);
            try
            {
                // Let the user finish releasing the mouse/key that triggered playback.
                PreciseSleep(250);

                for (int iteration = 1; !_stop && (_repeat == 0 || iteration <= _repeat); iteration++)
                {
                    var started = LoopStarted;
                    if (started != null) started(iteration);

                    int lastPercent = -1;
                    for (int i = 0; i < _events.Count; i++)
                    {
                        var e = _events[i];
                        if (_stop) break;
                        if (_speed > 0 && e.Delay > 0) PreciseSleep(e.Delay / _speed);
                        if (_stop) break;
                        Send(e);

                        // Only report whole percentage points — a macro can hold thousands of events.
                        int percent = (i + 1) * 100 / _events.Count;
                        if (percent != lastPercent)
                        {
                            lastPercent = percent;
                            var progressed = Progressed;
                            if (progressed != null) progressed(percent / 100.0);
                        }
                    }
                }
            }
            finally
            {
                ReleaseHeldKeys();
                Native.timeEndPeriod(1);
                var done = Finished;
                if (done != null) done();
            }
        }

        private void Send(MacroEvent e)
        {
            switch (e.Kind)
            {
                case EvKind.MouseMove:
                    SendMouse(Native.MOUSEEVENTF_MOVE | Native.MOUSEEVENTF_ABSOLUTE | Native.MOUSEEVENTF_VIRTUALDESK,
                              0, e.A, e.B);
                    break;
                case EvKind.MouseButton:
                    // A move event is always recorded immediately before a click,
                    // so the button press only needs the flag and the X-button index.
                    SendMouse((uint)e.A, (uint)e.B, 0, 0, absolute: false);
                    break;
                case EvKind.MouseWheel:
                    SendMouse((uint)e.A, unchecked((uint)e.B), 0, 0, absolute: false);
                    break;
                case EvKind.KeyDown:
                    SendKey(e, false);
                    _keysDown.Add(e.A);
                    break;
                case EvKind.KeyUp:
                    SendKey(e, true);
                    _keysDown.Remove(e.A);
                    break;
            }
        }

        private static void SendMouse(uint flags, uint data, int x, int y, bool absolute = true)
        {
            var input = new Native.INPUT { type = Native.INPUT_MOUSE };
            input.u.mi = new Native.MOUSEINPUT
            {
                dx = absolute ? Normalize(x, Native.GetSystemMetrics(Native.SM_XVIRTUALSCREEN), Native.GetSystemMetrics(Native.SM_CXVIRTUALSCREEN)) : 0,
                dy = absolute ? Normalize(y, Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN), Native.GetSystemMetrics(Native.SM_CYVIRTUALSCREEN)) : 0,
                mouseData = data,
                dwFlags = flags,
                time = 0,
                dwExtraInfo = Native.SelfTag
            };
            Native.SendInput(1, new[] { input }, Marshal.SizeOf(typeof(Native.INPUT)));
        }

        private static int Normalize(int value, int origin, int size)
        {
            if (size <= 1) return 0;
            long n = ((long)(value - origin) * 65535) / (size - 1);
            if (n < 0) n = 0;
            if (n > 65535) n = 65535;
            return (int)n;
        }

        private void SendKey(MacroEvent e, bool up)
        {
            uint flags = 0;
            if (up) flags |= Native.KEYEVENTF_KEYUP;
            if (e.C != 0) flags |= Native.KEYEVENTF_EXTENDEDKEY;
            if (_useScanCodes && e.B != 0) flags |= Native.KEYEVENTF_SCANCODE;

            var input = new Native.INPUT { type = Native.INPUT_KEYBOARD };
            input.u.ki = new Native.KEYBDINPUT
            {
                wVk = (flags & Native.KEYEVENTF_SCANCODE) != 0 ? (ushort)0 : (ushort)e.A,
                wScan = (ushort)e.B,
                dwFlags = flags,
                time = 0,
                dwExtraInfo = Native.SelfTag
            };
            Native.SendInput(1, new[] { input }, Marshal.SizeOf(typeof(Native.INPUT)));
        }

        // If playback is aborted mid-macro, don't leave Ctrl/Alt/Shift/etc. stuck down.
        private void ReleaseHeldKeys()
        {
            foreach (int vk in new List<int>(_keysDown))
                SendKey(new MacroEvent(EvKind.KeyUp, 0, vk, 0, 0), true);
            _keysDown.Clear();
        }

        private void PreciseSleep(double ms)
        {
            if (ms <= 0) return;
            var sw = Stopwatch.StartNew();
            while (!_stop)
            {
                double remaining = ms - sw.Elapsed.TotalMilliseconds;
                if (remaining <= 0) break;
                if (remaining > 25) Thread.Sleep(10);
                else if (remaining > 2) Thread.Sleep(1);
                else Thread.SpinWait(100);
            }
        }
    }

    #endregion

    internal sealed class MainForm : DtWindow
    {
        private const int HOTKEY_RECORD = 1;
        private const int HOTKEY_PLAY = 2;
        private const int VK_F9 = 0x78;
        private const int VK_F10 = 0x79;
        private const int VK_ESCAPE = 0x1B;

        private IntPtr _mouseHook, _keyboardHook;
        private Native.HookProc _mouseProc, _keyboardProc; // kept alive against the GC
        private readonly Stopwatch _clock = new Stopwatch();
        private long _lastEventTime;

        private List<MacroEvent> _events = new List<MacroEvent>();
        private bool _recording;
        private Player _player;
        private string _currentFile;
        private Point _lastRecordedPos = new Point(int.MinValue, int.MinValue);

        private Button _btnRecord, _btnPlay, _btnActions, _btnOpen, _btnSave;
        private bool _modalOpen;
        private NumericUpDown _numRepeat;
        private Button _btnSpeed;
        private int _speedIndex = 1;
        private static readonly string[] SpeedNames = { "0.5x", "1x", "2x", "4x", "Max" };
        private string SpeedLabel() { return SpeedNames[_speedIndex] + "   ▾"; }
        private CheckBox _chkMoves, _chkTopMost, _chkScanCodes;
        private Panel _pnlStatus, _pnlProgress;
        private Label _lblStatus, _lblMeta, _lblInfo;
        private System.Windows.Forms.Timer _blink;
        private bool _blinkOn;
        private string _playMeta = "";

        public MainForm()
        {
            BuildUi();
            InstallHooks();
            RegisterHotkeys();
            UpdateUi();
        }

        #region UI

        private void BuildUi()
        {
            Text = "Dolftasker.2";
            WindowTitle = "Dolftasker.2";
            ShowThemeToggle = true;
            TopMost = true;

            const int pad = 10, gap = 8, width = 380;
            const int content = width - pad * 2;
            int y = TitleBarHeight + pad;

            // Record / Play — Play is the one signal element on this surface.
            int half = (content - 6) / 2;
            _btnRecord = Dw.QuietButton("● Record");
            _btnRecord.SetBounds(pad, y, half, 32);
            _btnRecord.Click += (s, e) => ToggleRecord();
            Controls.Add(_btnRecord);

            _btnPlay = Dw.SignalButton("▶  Play");
            _btnPlay.SetBounds(pad + half + 6, y, content - half - 6, 32);
            _btnPlay.Click += (s, e) => TogglePlay();
            Controls.Add(_btnPlay);
            y += 32 + gap;

            // Bordered status strip: state left, detail right, progress rule beneath.
            _pnlStatus = new Panel { BackColor = Dw.Well, Tag = DwRole.StatusPanel };
            _pnlStatus.SetBounds(pad, y, content, 30);
            _pnlStatus.Paint += (s, e) =>
            {
                using (var pen = new Pen(Dw.Hairline))
                    e.Graphics.DrawRectangle(pen, 0, 0, _pnlStatus.Width - 1, _pnlStatus.Height - 1);
            };
            Controls.Add(_pnlStatus);

            _lblStatus = Dw.Text("READY", 7, 5, 150, Dw.Ink, Dw.MonoBold);
            _pnlStatus.Controls.Add(_lblStatus);

            _lblMeta = Dw.Text("", content - 217, 5, 210, Dw.Muted, Dw.Mono);
            _lblMeta.TextAlign = ContentAlignment.MiddleRight;
            _pnlStatus.Controls.Add(_lblMeta);

            var track = new Panel { BackColor = Dw.Hairline, Tag = DwRole.Track };
            track.SetBounds(1, 27, content - 2, 2);
            _pnlStatus.Controls.Add(track);
            _pnlProgress = new Panel { BackColor = Dw.Signal, Tag = DwRole.Progress };
            _pnlProgress.SetBounds(0, 0, 0, 2);
            track.Controls.Add(_pnlProgress);
            y += 30 + gap;

            // Playback
            var playback = Dw.Group("Playback", pad, y, content, 68);
            Controls.Add(playback);

            playback.Controls.Add(Dw.Text("Repeat", 8, 18, 46, Dw.Second, Dw.Ui));

            // A borderless spinner inside a 1px panel — the only way to get a border
            // in the palette's colour without owner-drawing the control.
            _numRepeat = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100000,
                Value = 1,
                Font = Dw.Ui,
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Center,
                Dock = DockStyle.Fill,
                Tag = DwRole.Field
            };
            Dw.StyleField(_numRepeat);
            var repeatFrame = new Panel { BackColor = Dw.Hairline, Padding = new Padding(1), Tag = DwRole.Track };
            repeatFrame.SetBounds(56, 16, 72, 22);
            repeatFrame.Controls.Add(_numRepeat);
            playback.Controls.Add(repeatFrame);

            playback.Controls.Add(Dw.Text("Speed", 142, 18, 44, Dw.Second, Dw.Ui));

            // The design specifies a cycling control here, not a drop-down — which also
            // means a stock Button, so it themes properly. A combo's arrow cannot.
            _btnSpeed = Dw.QuietButton(SpeedLabel());
            _btnSpeed.SetBounds(190, 16, 88, 22);
            _btnSpeed.TextAlign = ContentAlignment.MiddleLeft;
            _btnSpeed.Padding = new Padding(6, 0, 0, 0);
            _btnSpeed.Click += (s, e) =>
            {
                _speedIndex = (_speedIndex + 1) % SpeedNames.Length;
                _btnSpeed.Text = SpeedLabel();
            };
            playback.Controls.Add(_btnSpeed);

            playback.Controls.Add(Dw.Text("0 = loop until Esc", 8, 44, 200, Dw.Muted, Dw.UiSmall));

            y += playback.Height + gap;

            // Capture
            var capture = Dw.Group("Capture", pad, y, content, 88);
            Controls.Add(capture);

            _chkMoves = Dw.Check("Record mouse movement", 10, 18, content - 24, true);
            capture.Controls.Add(_chkMoves);
            _chkTopMost = Dw.Check("Always on top", 10, 40, content - 24, true);
            _chkTopMost.CheckedChanged += (s, e) => TopMost = _chkTopMost.Checked;
            capture.Controls.Add(_chkTopMost);
            _chkScanCodes = Dw.Check("Game mode (scan codes)", 10, 62, content - 24, false);
            capture.Controls.Add(_chkScanCodes);

            y += capture.Height + gap;

            // Macro
            var macro = Dw.Group("Macro", pad, y, content, 48);
            Controls.Add(macro);

            int cell = (content - 16 - 12) / 3;
            _btnActions = AddMacroButton(macro, "Actions…", 8, cell);
            _btnActions.Click += (s, e) => EditActions();
            _btnOpen = AddMacroButton(macro, "Open…", 8 + cell + 6, cell);
            _btnOpen.Click += (s, e) => OpenFile();
            _btnSave = AddMacroButton(macro, "Save…", 8 + (cell + 6) * 2, cell);
            _btnSave.Click += (s, e) => SaveFile();

            y += macro.Height + gap;

            _lblInfo = Dw.Text("F9 record     F10 play     Esc stop", pad + 1, y, content, Dw.Muted, Dw.Mono);
            Controls.Add(_lblInfo);
            y += 16 + pad;

            ClientSize = new Size(width, y);
        }

        private Button AddMacroButton(Control parent, string text, int x, int width)
        {
            var b = Dw.QuietButton(text);
            b.SetBounds(x, 18, width, 26);
            parent.Controls.Add(b);
            return b;
        }

        private void UpdateUi()
        {
            bool playing = _player != null && _player.IsRunning;
            bool idle = !_recording && !playing;

            _btnRecord.Text = _recording ? "● Stop recording" : "● Record";
            _btnRecord.Enabled = !playing;
            SetBlinking(_recording);

            _btnPlay.Text = playing ? "■  Stop" : "▶  Play";
            _btnPlay.Enabled = !_recording && _events.Count > 0;
            _btnPlay.BackColor = _btnPlay.Enabled ? Dw.Signal : Dw.SignalMuted;
            _btnPlay.ForeColor = _btnPlay.Enabled ? Dw.White : Dw.OnSignalMuted;
            _btnPlay.FlatAppearance.BorderColor = _btnPlay.BackColor;

            _btnOpen.Enabled = idle;
            _btnSave.Enabled = idle && _events.Count > 0;
            _btnActions.Enabled = idle && _events.Count > 0;
            _numRepeat.Enabled = _btnSpeed.Enabled = !playing;

            if (_recording)
                SetStatus("RECORDING", _events.Count + " steps captured", 1, Dw.Error);
            else if (playing)
                SetStatus("PLAYING BACK", _playMeta, -1, Dw.Signal);
            else if (_events.Count == 0)
                SetStatus("READY", "no macro loaded", 0, Dw.Signal);
            else
                SetStatus(_currentFile == null ? "READY" : Path.GetFileNameWithoutExtension(_currentFile).ToUpperInvariant(),
                          _events.Count + " steps · " + (TotalMs() / 1000.0).ToString("0.00") + " s", 0, Dw.Signal);
        }

        // A theme switch resets colours from the palette; the run state owns the
        // Play fill, the status colour and the record dot, so reassert them.
        protected override void OnThemeChanged()
        {
            base.OnThemeChanged();
            UpdateUi();
        }

        /// <summary>Progress below zero leaves the current fill alone.</summary>
        private void SetStatus(string status, string meta, double progress, Color accent)
        {
            _lblStatus.Text = status;
            _lblStatus.ForeColor = _recording ? Dw.Error : Dw.Ink;
            _lblMeta.Text = meta;
            _pnlProgress.BackColor = accent;
            if (progress >= 0) SetProgress(progress);
        }

        private void SetProgress(double fraction)
        {
            var track = _pnlProgress.Parent;
            if (track == null) return;
            _pnlProgress.Width = (int)Math.Round(track.Width * Math.Max(0, Math.Min(1, fraction)));
        }

        // The record dot is the only sanctioned use of error red outside errors —
        // recording is a state you must not miss.
        private void SetBlinking(bool on)
        {
            if (on)
            {
                if (_blink == null)
                {
                    _blink = new System.Windows.Forms.Timer { Interval = 500 };
                    _blink.Tick += (s, e) =>
                    {
                        _blinkOn = !_blinkOn;
                        _btnRecord.ForeColor = _blinkOn ? Dw.Error : Dw.Ink;
                    };
                }
                _blinkOn = true;
                _btnRecord.ForeColor = Dw.Error;
                _blink.Start();
            }
            else
            {
                if (_blink != null) _blink.Stop();
                _btnRecord.ForeColor = Dw.Ink;
            }
        }

        private long TotalMs()
        {
            long total = 0;
            foreach (var e in _events) total += e.Delay;
            return total;
        }

        #endregion

        #region Hooks / recording

        private void InstallHooks()
        {
            _mouseProc = MouseHookProc;
            _keyboardProc = KeyboardHookProc;
            IntPtr module = Native.GetModuleHandle(null);
            _mouseHook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, _mouseProc, module, 0);
            _keyboardHook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _keyboardProc, module, 0);
        }

        private void RegisterHotkeys()
        {
            bool a = Native.RegisterHotKey(Handle, HOTKEY_RECORD, 0, VK_F9);
            bool b = Native.RegisterHotKey(Handle, HOTKEY_PLAY, 0, VK_F10);
            if (!a || !b)
                _lblInfo.Text = "Hotkey in use by another app — use the buttons instead.";
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY && !_modalOpen)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_RECORD) ToggleRecord();
                else if (id == HOTKEY_PLAY) TogglePlay();
            }
            base.WndProc(ref m);
        }

        private int NextDelay()
        {
            long now = _clock.ElapsedMilliseconds;
            int delay = (int)(now - _lastEventTime);
            _lastEventTime = now;
            return delay < 0 ? 0 : delay;
        }

        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var data = (Native.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Native.MSLLHOOKSTRUCT));
                bool injected = (data.flags & Native.LLMHF_INJECTED) != 0 || data.dwExtraInfo == Native.SelfTag;

                if (!injected && wParam.ToInt32() == Native.WM_MOUSEMOVE
                    && _player != null && _player.IsRunning)
                {
                    // A real move is the user taking the mouse back — stop before the
                    // macro fights them for it, same as a physical Esc during playback.
                    BeginInvoke((Action)StopPlayback);
                }
                else if (_recording && !injected)
                {
                    RecordMouse(wParam.ToInt32(), data);
                }
            }
            return Native.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private void RecordMouse(int msg, Native.MSLLHOOKSTRUCT data)
        {
            int mouseData = unchecked((int)data.mouseData);
            short wheelDelta = (short)((mouseData >> 16) & 0xFFFF);
            int xButton = (mouseData >> 16) & 0xFFFF;

            switch (msg)
            {
                case Native.WM_MOUSEMOVE:
                    if (!_chkMoves.Checked) return;
                    AddMove(data.pt.x, data.pt.y);
                    return;

                case Native.WM_MOUSEWHEEL:
                    _events.Add(new MacroEvent(EvKind.MouseWheel, NextDelay(), (int)Native.MOUSEEVENTF_WHEEL, wheelDelta, 0));
                    return;
                case Native.WM_MOUSEHWHEEL:
                    _events.Add(new MacroEvent(EvKind.MouseWheel, NextDelay(), (int)Native.MOUSEEVENTF_HWHEEL, wheelDelta, 0));
                    return;
            }

            uint flag;
            uint extra = 0;
            switch (msg)
            {
                case Native.WM_LBUTTONDOWN: flag = Native.MOUSEEVENTF_LEFTDOWN; break;
                case Native.WM_LBUTTONUP: flag = Native.MOUSEEVENTF_LEFTUP; break;
                case Native.WM_RBUTTONDOWN: flag = Native.MOUSEEVENTF_RIGHTDOWN; break;
                case Native.WM_RBUTTONUP: flag = Native.MOUSEEVENTF_RIGHTUP; break;
                case Native.WM_MBUTTONDOWN: flag = Native.MOUSEEVENTF_MIDDLEDOWN; break;
                case Native.WM_MBUTTONUP: flag = Native.MOUSEEVENTF_MIDDLEUP; break;
                case Native.WM_XBUTTONDOWN: flag = Native.MOUSEEVENTF_XDOWN; extra = (uint)xButton; break;
                case Native.WM_XBUTTONUP: flag = Native.MOUSEEVENTF_XUP; extra = (uint)xButton; break;
                default: return;
            }

            // Always pin the click to the exact spot it happened, even if moves aren't recorded.
            if (_lastRecordedPos.X != data.pt.x || _lastRecordedPos.Y != data.pt.y)
                AddMove(data.pt.x, data.pt.y, force: true);

            _events.Add(new MacroEvent(EvKind.MouseButton, NextDelay(), (int)flag, (int)extra, 0));
        }

        private void AddMove(int x, int y, bool force = false)
        {
            if (!force && _lastRecordedPos.X == x && _lastRecordedPos.Y == y) return;

            // Collapse a burst of moves into one event so recordings stay small.
            int count = _events.Count;
            if (!force && count > 0 && _events[count - 1].Kind == EvKind.MouseMove)
            {
                long sinceLast = _clock.ElapsedMilliseconds - _lastEventTime;
                if (sinceLast < 8)
                {
                    var prev = _events[count - 1];
                    prev.A = x; prev.B = y;
                    _events[count - 1] = prev;
                    _lastRecordedPos = new Point(x, y);
                    return;
                }
            }

            _events.Add(new MacroEvent(EvKind.MouseMove, NextDelay(), x, y, 0));
            _lastRecordedPos = new Point(x, y);
        }

        private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var data = (Native.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Native.KBDLLHOOKSTRUCT));
                bool injected = (data.flags & Native.LLKHF_INJECTED) != 0 || data.dwExtraInfo == Native.SelfTag;
                int msg = wParam.ToInt32();
                bool isDown = msg == Native.WM_KEYDOWN || msg == Native.WM_SYSKEYDOWN;

                // A real Escape keypress aborts playback.
                if (!injected && isDown && data.vkCode == VK_ESCAPE && _player != null && _player.IsRunning)
                {
                    BeginInvoke((Action)StopPlayback);
                }
                else if (_recording && !injected && data.vkCode != VK_F9 && data.vkCode != VK_F10)
                {
                    bool extended = (data.flags & Native.LLKHF_EXTENDED) != 0;
                    _events.Add(new MacroEvent(
                        isDown ? EvKind.KeyDown : EvKind.KeyUp,
                        NextDelay(),
                        (int)data.vkCode,
                        (int)data.scanCode,
                        extended ? 1 : 0));
                }
            }
            return Native.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        #endregion

        #region Actions

        private void ToggleRecord()
        {
            if (_player != null && _player.IsRunning) return;

            if (_recording)
            {
                _recording = false;
                _clock.Stop();
                TrimTrailingMoves();
            }
            else
            {
                _events = new List<MacroEvent>();
                _currentFile = null;
                _lastRecordedPos = new Point(int.MinValue, int.MinValue);
                _lastEventTime = 0;
                _clock.Restart();
                _recording = true;
            }
            UpdateUi();
        }

        // The mouse travel back to the Stop button isn't part of the macro.
        private void TrimTrailingMoves()
        {
            while (_events.Count > 0 && _events[_events.Count - 1].Kind == EvKind.MouseMove)
                _events.RemoveAt(_events.Count - 1);
        }

        private void TogglePlay()
        {
            if (_recording) return;

            if (_player != null && _player.IsRunning)
            {
                StopPlayback();
                return;
            }
            if (_events.Count == 0) return;

            double speed;
            switch (_speedIndex)
            {
                case 0: speed = 0.5; break;
                case 2: speed = 2; break;
                case 3: speed = 4; break;
                case 4: speed = 0; break; // no delays
                default: speed = 1; break;
            }

            int repeat = (int)_numRepeat.Value;
            _player = new Player(_events, repeat, speed, _chkScanCodes.Checked);
            _player.LoopStarted += i => BeginInvoke((Action)(() =>
            {
                _playMeta = "pass " + i + " of " + (repeat == 0 ? "∞" : repeat.ToString());
                UpdateUi();
            }));
            _player.Progressed += p => BeginInvoke((Action)(() => SetProgress(p)));
            _player.Finished += () => BeginInvoke((Action)(() =>
            {
                SetProgress(0);
                UpdateUi();
            }));
            _player.Start();
            UpdateUi();
        }

        private void StopPlayback()
        {
            if (_player != null) _player.Stop();
            UpdateUi();
        }

        private void EditActions()
        {
            if (_recording || (_player != null && _player.IsRunning) || _events.Count == 0) return;

            _modalOpen = true;
            try
            {
                using (var dlg = new ActionsForm(new List<MacroEvent>(_events), (int)_numRepeat.Value))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        _events = dlg.EditedEvents;
                }
            }
            finally { _modalOpen = false; }

            UpdateUi();
        }

        private void OpenFile()
        {
            _modalOpen = true;
            try { OpenFileCore(); }
            finally { _modalOpen = false; }
        }

        private void OpenFileCore()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Dolftasker.2 recording (*.rec)|*.rec|All files (*.*)|*.*";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    _events = MacroFile.Load(dlg.FileName);
                    _currentFile = dlg.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Open failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                UpdateUi();
            }
        }

        private void SaveFile()
        {
            _modalOpen = true;
            try { SaveFileCore(); }
            finally { _modalOpen = false; }
        }

        private void SaveFileCore()
        {
            if (_events.Count == 0) return;
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "Dolftasker.2 recording (*.rec)|*.rec";
                dlg.DefaultExt = "rec";
                dlg.FileName = _currentFile != null ? Path.GetFileName(_currentFile) : "macro.rec";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    MacroFile.Save(dlg.FileName, _events);
                    _currentFile = dlg.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                UpdateUi();
            }
        }

        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_player != null) _player.Stop();
            Native.UnregisterHotKey(Handle, HOTKEY_RECORD);
            Native.UnregisterHotKey(Handle, HOTKEY_PLAY);
            if (_mouseHook != IntPtr.Zero) Native.UnhookWindowsHookEx(_mouseHook);
            if (_keyboardHook != IntPtr.Zero) Native.UnhookWindowsHookEx(_keyboardHook);
            base.OnFormClosing(e);
        }
    }

    #region Actions editor

    // Translates between MacroEvents and the human-readable rows shown in the editor.
    internal static class ActionNames
    {
        public const string Move = "Mouse Move";
        public const string Wheel = "Wheel";
        public const string HWheel = "Wheel (horiz)";
        public const string KeyDown = "Key Down";
        public const string KeyUp = "Key Up";

        public static readonly string[] All =
        {
            Move,
            "Left Down", "Left Up",
            "Right Down", "Right Up",
            "Middle Down", "Middle Up",
            "X1 Down", "X1 Up",
            "X2 Down", "X2 Up",
            Wheel, HWheel,
            KeyDown, KeyUp
        };

        public static string Of(MacroEvent e)
        {
            switch (e.Kind)
            {
                case EvKind.MouseMove: return Move;
                case EvKind.KeyDown: return KeyDown;
                case EvKind.KeyUp: return KeyUp;
                case EvKind.MouseWheel:
                    return (uint)e.A == Native.MOUSEEVENTF_HWHEEL ? HWheel : Wheel;
                case EvKind.MouseButton:
                    switch ((uint)e.A)
                    {
                        case Native.MOUSEEVENTF_LEFTDOWN: return "Left Down";
                        case Native.MOUSEEVENTF_LEFTUP: return "Left Up";
                        case Native.MOUSEEVENTF_RIGHTDOWN: return "Right Down";
                        case Native.MOUSEEVENTF_RIGHTUP: return "Right Up";
                        case Native.MOUSEEVENTF_MIDDLEDOWN: return "Middle Down";
                        case Native.MOUSEEVENTF_MIDDLEUP: return "Middle Up";
                        case Native.MOUSEEVENTF_XDOWN: return e.B == 2 ? "X2 Down" : "X1 Down";
                        case Native.MOUSEEVENTF_XUP: return e.B == 2 ? "X2 Up" : "X1 Up";
                    }
                    break;
            }
            return Move;
        }

        // Sets Kind/A/B on the event to match the chosen action name.
        public static void Apply(ref MacroEvent e, string name)
        {
            switch (name)
            {
                case Move: e.Kind = EvKind.MouseMove; break;
                case KeyDown: e.Kind = EvKind.KeyDown; break;
                case KeyUp: e.Kind = EvKind.KeyUp; break;
                case Wheel: e.Kind = EvKind.MouseWheel; e.A = (int)Native.MOUSEEVENTF_WHEEL; break;
                case HWheel: e.Kind = EvKind.MouseWheel; e.A = (int)Native.MOUSEEVENTF_HWHEEL; break;
                default:
                    e.Kind = EvKind.MouseButton;
                    e.B = 0;
                    switch (name)
                    {
                        case "Left Down": e.A = (int)Native.MOUSEEVENTF_LEFTDOWN; break;
                        case "Left Up": e.A = (int)Native.MOUSEEVENTF_LEFTUP; break;
                        case "Right Down": e.A = (int)Native.MOUSEEVENTF_RIGHTDOWN; break;
                        case "Right Up": e.A = (int)Native.MOUSEEVENTF_RIGHTUP; break;
                        case "Middle Down": e.A = (int)Native.MOUSEEVENTF_MIDDLEDOWN; break;
                        case "Middle Up": e.A = (int)Native.MOUSEEVENTF_MIDDLEUP; break;
                        case "X1 Down": e.A = (int)Native.MOUSEEVENTF_XDOWN; e.B = 1; break;
                        case "X2 Down": e.A = (int)Native.MOUSEEVENTF_XDOWN; e.B = 2; break;
                        case "X1 Up": e.A = (int)Native.MOUSEEVENTF_XUP; e.B = 1; break;
                        case "X2 Up": e.A = (int)Native.MOUSEEVENTF_XUP; e.B = 2; break;
                    }
                    break;
            }
        }

        public static bool IsMove(string n) { return n == Move; }
        public static bool IsKey(string n) { return n == KeyDown || n == KeyUp; }
        public static bool IsWheel(string n) { return n == Wheel || n == HWheel; }
    }

    internal static class KeyInfo
    {
        private static readonly int[] ExtendedVks =
        {
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, // PgUp/PgDn/End/Home/arrows
            0x2D, 0x2E,                                     // Insert, Delete
            0x90, 0x6F,                                     // NumLock, Divide
            0xA3, 0xA5, 0x5B, 0x5C, 0x5D                    // RCtrl, RAlt, LWin, RWin, Apps
        };

        public static bool IsExtended(int vk)
        {
            foreach (int v in ExtendedVks) if (v == vk) return true;
            return false;
        }

        public static int ScanCode(int vk)
        {
            return (int)Native.MapVirtualKey((uint)vk, Native.MAPVK_VK_TO_VSC);
        }

        public static string Name(int vk)
        {
            if (vk <= 0 || vk > 255) return vk.ToString();
            return ((Keys)vk).ToString();
        }

        public static int Parse(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            try { return (int)(Keys)Enum.Parse(typeof(Keys), name, true); }
            catch
            {
                int n;
                return int.TryParse(name, out n) ? n : 0;
            }
        }

        // Every single-byte virtual key, for the drop-down.
        public static List<string> AllNames()
        {
            var names = new List<string>();
            foreach (string n in Enum.GetNames(typeof(Keys)))
            {
                int v = (int)(Keys)Enum.Parse(typeof(Keys), n);
                if (v > 0 && v <= 255) names.Add(n);
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            names.Insert(0, "");
            return names;
        }
    }

    internal sealed class ActionsForm : DtWindow
    {
        private const string NotApplicable = "—";

        private readonly DataGridView _grid = new DataGridView();
        private DataGridViewComboBoxColumn _colKey;
        private Label _lblSummary;
        private readonly int _repeat;
        private bool _loading;

        /// <summary>The edited list, valid once the dialog returns OK.</summary>
        public List<MacroEvent> EditedEvents { get; private set; }

        public ActionsForm(List<MacroEvent> events, int repeat)
        {
            EditedEvents = events;
            _repeat = repeat;
            BuildUi();
            LoadRows(events);
            UpdateSummary();
        }

        private void BuildUi()
        {
            Text = "Dolftasker.2 — Actions";
            WindowTitle = "Dolftasker.2 — Actions";
            ShowMinimise = false;
            Resizable = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            const int pad = 10, gap = 8;
            ClientSize = new Size(760, 470);
            MinimumSize = new Size(620, 320);

            int top = TitleBarHeight + pad;
            int content = ClientSize.Width - pad * 2;

            // Toolbar: quiet controls left, the run summary right.
            int x = pad;
            x += AddToolButton("Insert", x, top, 62, (s, e) => InsertRow());
            x += AddToolButton("Delete", x, top, 62, (s, e) => DeleteRows());
            x += AddToolButton("▲", x, top, 30, (s, e) => MoveRow(-1));
            x += AddToolButton("▼", x, top, 30, (s, e) => MoveRow(1));

            _lblSummary = Dw.Text("", x + gap, top, ClientSize.Width - pad - (x + gap), Dw.Muted, Dw.Mono);
            _lblSummary.Height = 26;
            _lblSummary.TextAlign = ContentAlignment.MiddleRight;
            _lblSummary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(_lblSummary);

            int gridTop = top + 26 + gap;
            const int footerHeight = 26;
            BuildGrid(pad, gridTop, content, ClientSize.Height - gridTop - footerHeight - gap - pad);

            int footerY = ClientSize.Height - pad - footerHeight;
            var hint = Dw.Text("Wait is the pause before that step. Cells that don't apply read " + NotApplicable + ".",
                               pad, footerY, content - 190, Dw.Muted, Dw.Ui);
            hint.Height = footerHeight;
            hint.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Controls.Add(hint);

            var btnApply = Dw.InkButton("Apply");
            btnApply.SetBounds(ClientSize.Width - pad - 76, footerY, 76, footerHeight);
            btnApply.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnApply.Click += (s, e) => Apply();
            Controls.Add(btnApply);

            var btnCancel = Dw.QuietButton("Cancel");
            btnCancel.SetBounds(ClientSize.Width - pad - 76 - gap - 72, footerY, 72, footerHeight);
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnCancel);
        }

        private int AddToolButton(string text, int x, int y, int width, EventHandler onClick)
        {
            var b = Dw.QuietButton(text);
            b.SetBounds(x, y, width, 26);
            b.Click += onClick;
            Controls.Add(b);
            return width + 6;
        }

        private void BuildGrid(int x, int y, int width, int height)
        {
            _grid.Location = new Point(x, y);
            _grid.Size = new Size(width, height);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.AllowUserToResizeColumns = false;
            _grid.RowHeadersVisible = false;
            _grid.BackgroundColor = Dw.Surface;
            _grid.BorderStyle = BorderStyle.FixedSingle;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.EditMode = DataGridViewEditMode.EditOnEnter;
            // Fill mode keeps the columns flush with the right edge at any window size.
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _grid.EnableHeadersVisualStyles = false;
            _grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            _grid.GridColor = Dw.LineSoft;
            _grid.ColumnHeadersHeight = 30;
            _grid.RowTemplate.Height = 26;
            _grid.Font = Dw.Ui;

            _grid.ColumnHeadersDefaultCellStyle.BackColor = Dw.Well;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Dw.Muted;
            _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Dw.Well;
            _grid.ColumnHeadersDefaultCellStyle.Font = Dw.MonoBold;
            _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            _grid.DefaultCellStyle.BackColor = Dw.Surface;
            _grid.DefaultCellStyle.ForeColor = Dw.Second;
            _grid.DefaultCellStyle.SelectionBackColor = Dw.Well;
            _grid.DefaultCellStyle.SelectionForeColor = Dw.Ink;
            _grid.DefaultCellStyle.Font = Dw.Mono;
            _grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);

            // "#" carries the run boundaries: FIRST and LAST in signal, the only blue here.
            var idx = AddTextColumn("idx", "#", 112, readOnly: true);
            idx.DefaultCellStyle.Font = Dw.MonoBold;

            var colAct = new DataGridViewComboBoxColumn
            {
                Name = "act",
                HeaderText = "ACTION",
                Width = 160,
                FlatStyle = FlatStyle.Flat,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing,
                DisplayStyleForCurrentCellOnly = true
            };
            colAct.Items.AddRange(ActionNames.All);
            colAct.DefaultCellStyle.Font = Dw.Ui;
            colAct.DefaultCellStyle.ForeColor = Dw.Ink;
            _grid.Columns.Add(colAct);

            AddTextColumn("x", "X", 76, false, right: true);
            AddTextColumn("y", "Y", 76, false, right: true);

            _colKey = new DataGridViewComboBoxColumn
            {
                Name = "key",
                HeaderText = "KEY",
                Width = 140,
                FlatStyle = FlatStyle.Flat,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing,
                DisplayStyleForCurrentCellOnly = true
            };
            _colKey.Items.AddRange(KeyInfo.AllNames().ToArray());
            _colKey.Items.Add(NotApplicable);
            _grid.Columns.Add(_colKey);

            AddTextColumn("wheel", "WHEEL", 84, false, right: true);
            var wait = AddTextColumn("wait", "WAIT MS", 100, false, right: true);
            wait.DefaultCellStyle.ForeColor = Dw.Ink;
            wait.DefaultCellStyle.Font = Dw.MonoBold;

            _grid.CellValueChanged += Grid_CellValueChanged;
            _grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_grid.IsCurrentCellDirty)
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.DataError += (s, e) => { e.ThrowException = false; };
            _grid.UserDeletedRow += (s, e) => { Renumber(); UpdateSummary(); };
            _grid.EditingControlShowing += Grid_EditingControlShowing;
            Dw.StyleGrid(_grid);
            Controls.Add(_grid);
        }

        private DataGridViewTextBoxColumn AddTextColumn(string name, string header, int width, bool readOnly, bool right = false)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                Width = width,
                ReadOnly = readOnly,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            if (right) col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grid.Columns.Add(col);
            return col;
        }

        private void Grid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            e.CellStyle.BackColor = Dw.Surface;
            e.CellStyle.ForeColor = Dw.Ink;
            var combo = e.Control as ComboBox;
            if (combo != null)
            {
                combo.BackColor = Dw.Surface;
                combo.ForeColor = Dw.Ink;
                combo.FlatStyle = FlatStyle.Flat;
            }
            var box = e.Control as TextBox;
            if (box != null)
            {
                box.BackColor = Dw.Surface;
                box.ForeColor = Dw.Ink;
            }
        }

        #region Rows

        private void LoadRows(List<MacroEvent> events)
        {
            _loading = true;
            _grid.Rows.Clear();
            foreach (var e in events) AppendRow(e);
            Renumber();
            _loading = false;
        }

        private int AppendRow(MacroEvent e)
        {
            int index = _grid.Rows.Add();
            FillRow(_grid.Rows[index], e);
            return index;
        }

        private void FillRow(DataGridViewRow row, MacroEvent e)
        {
            string action = ActionNames.Of(e);
            row.Tag = e; // keeps the original scan code unless the key is changed
            row.Cells["act"].Value = action;
            row.Cells["wait"].Value = e.Delay;

            if (e.Kind == EvKind.MouseMove)
            {
                row.Cells["x"].Value = e.A;
                row.Cells["y"].Value = e.B;
            }
            else if (e.Kind == EvKind.KeyDown || e.Kind == EvKind.KeyUp)
            {
                string name = KeyInfo.Name(e.A);
                if (!_colKey.Items.Contains(name)) _colKey.Items.Add(name);
                row.Cells["key"].Value = name;
            }
            else if (e.Kind == EvKind.MouseWheel)
            {
                row.Cells["wheel"].Value = e.B;
            }

            StyleRow(row);
        }

        // Cells that don't apply to a row read "—" and can't be edited.
        private void StyleRow(DataGridViewRow row)
        {
            string action = Convert.ToString(row.Cells["act"].Value);
            SetEnabled(row.Cells["x"], ActionNames.IsMove(action));
            SetEnabled(row.Cells["y"], ActionNames.IsMove(action));
            SetEnabled(row.Cells["key"], ActionNames.IsKey(action));
            SetEnabled(row.Cells["wheel"], ActionNames.IsWheel(action));
        }

        private static void SetEnabled(DataGridViewCell cell, bool enabled)
        {
            cell.ReadOnly = !enabled;
            cell.Style.ForeColor = enabled ? Dw.Second : Dw.Muted;
            if (!enabled) cell.Value = NotApplicable;
            else if (Convert.ToString(cell.Value) == NotApplicable)
                cell.Value = cell is DataGridViewComboBoxCell ? (object)"" : null;
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_loading || e.RowIndex < 0) return;
            var row = _grid.Rows[e.RowIndex];

            if (_grid.Columns[e.ColumnIndex].Name == "act")
            {
                string action = Convert.ToString(row.Cells["act"].Value);
                StyleRow(row);

                // Give the newly relevant cells a usable starting value.
                if (ActionNames.IsKey(action) && IsBlank(row.Cells["key"].Value))
                    row.Cells["key"].Value = "A";
                if (ActionNames.IsWheel(action) && IsBlank(row.Cells["wheel"].Value))
                    row.Cells["wheel"].Value = 120;
                if (ActionNames.IsMove(action))
                {
                    if (IsBlank(row.Cells["x"].Value)) row.Cells["x"].Value = 0;
                    if (IsBlank(row.Cells["y"].Value)) row.Cells["y"].Value = 0;
                }
            }

            UpdateSummary();
        }

        private static bool IsBlank(object value)
        {
            if (value == null) return true;
            string s = value.ToString();
            return s.Length == 0 || s == NotApplicable;
        }

        // The footer buttons are painted controls, not IButtonControls, so Enter and
        // Escape are wired up by hand.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Enter))
            {
                Apply();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Numbers the rows and flags where the macro starts and stops.
        private void Renumber()
        {
            int last = _grid.Rows.Count - 1;
            for (int i = 0; i <= last; i++)
            {
                var cell = _grid.Rows[i].Cells["idx"];
                string mark = last == 0 ? "  ONLY" : i == 0 ? "  FIRST" : i == last ? "  LAST" : "";
                cell.Value = (i + 1) + mark;
                cell.Style.ForeColor = mark.Length > 0 ? Dw.Signal : Dw.Muted;
            }
        }

        private void InsertRow()
        {
            int at = _grid.CurrentRow != null ? _grid.CurrentRow.Index + 1 : _grid.Rows.Count;
            _loading = true;
            _grid.Rows.Insert(at, 1);
            FillRow(_grid.Rows[at], new MacroEvent(EvKind.MouseButton, 100, (int)Native.MOUSEEVENTF_LEFTDOWN, 0, 0));
            _grid.Rows[at].Tag = null; // brand new, nothing to preserve
            _loading = false;
            Renumber();
            UpdateSummary();
            _grid.CurrentCell = _grid.Rows[at].Cells["act"];
        }

        private void DeleteRows()
        {
            var doomed = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in _grid.SelectedRows) doomed.Add(row);
            if (doomed.Count == 0 && _grid.CurrentRow != null) doomed.Add(_grid.CurrentRow);
            foreach (var row in doomed) _grid.Rows.Remove(row);
            Renumber();
            UpdateSummary();
        }

        private void MoveRow(int direction)
        {
            if (_grid.CurrentRow == null) return;
            int from = _grid.CurrentRow.Index;
            int to = from + direction;
            if (to < 0 || to >= _grid.Rows.Count) return;

            var row = _grid.Rows[from];
            _loading = true;
            _grid.Rows.RemoveAt(from);
            _grid.Rows.Insert(to, row);
            _loading = false;
            Renumber();
            _grid.CurrentCell = _grid.Rows[to].Cells["act"];
            _grid.Rows[to].Selected = true;
        }

        #endregion

        private void UpdateSummary()
        {
            long total = 0;
            foreach (DataGridViewRow row in _grid.Rows) total += ReadInt(row.Cells["wait"].Value, 0);
            _lblSummary.Text = string.Format("{0} steps · {1:0.00} s · repeat {2}",
                _grid.Rows.Count, total / 1000.0, _repeat == 0 ? "∞" : _repeat.ToString());
        }

        private static int ReadInt(object value, int fallback)
        {
            if (value == null) return fallback;
            int n;
            return int.TryParse(value.ToString().Trim(), out n) ? n : fallback;
        }

        private void Apply()
        {
            _grid.EndEdit();
            var list = new List<MacroEvent>(_grid.Rows.Count);

            foreach (DataGridViewRow row in _grid.Rows)
            {
                var original = row.Tag is MacroEvent ? (MacroEvent)row.Tag : new MacroEvent();
                var ev = new MacroEvent { Delay = Math.Max(0, ReadInt(row.Cells["wait"].Value, 0)) };

                string action = Convert.ToString(row.Cells["act"].Value);
                if (string.IsNullOrEmpty(action)) action = ActionNames.Move;
                ActionNames.Apply(ref ev, action);

                if (ev.Kind == EvKind.MouseMove)
                {
                    ev.A = ReadInt(row.Cells["x"].Value, 0);
                    ev.B = ReadInt(row.Cells["y"].Value, 0);
                }
                else if (ev.Kind == EvKind.MouseWheel)
                {
                    ev.B = ReadInt(row.Cells["wheel"].Value, 120);
                }
                else if (ev.Kind == EvKind.KeyDown || ev.Kind == EvKind.KeyUp)
                {
                    int vk = KeyInfo.Parse(Convert.ToString(row.Cells["key"].Value));
                    if (vk == 0) continue; // a key row with no key selected is dropped
                    ev.A = vk;

                    bool sameKey = (original.Kind == EvKind.KeyDown || original.Kind == EvKind.KeyUp)
                                   && original.A == vk;
                    if (sameKey)
                    {
                        ev.B = original.B; // keep the scan code exactly as recorded
                        ev.C = original.C;
                    }
                    else
                    {
                        ev.B = KeyInfo.ScanCode(vk);
                        ev.C = KeyInfo.IsExtended(vk) ? 1 : 0;
                    }
                }

                list.Add(ev);
            }

            EditedEvents = list;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    #endregion

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Remembered choice, else follow the Windows app-theme setting.
            Dw.P = (Settings.ReadDark() ?? Settings.OsPrefersDark()) ? DwPalette.Dark : DwPalette.Light;

            var form = new MainForm();
            Application.Run(form);
        }
    }
}
