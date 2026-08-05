// DOLFWA styling for Dolftasker.2.
// Palette, fonts and the 30px window chrome. Every widget is a stock WinForms
// control tinted from these values — text is rendered by GDI so it stays sharp.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DolfTask
{
    /// <summary>
    /// One theme's colours. Names are roles, not literal colours: in the dark
    /// palette "Ink" is a light text colour and "Paper" is a dark background.
    /// </summary>
    internal sealed class DwPalette
    {
        public bool IsDark;
        public Color Chrome;     // title bar
        public Color Paper;      // window background
        public Color Surface;    // controls, cards, grid rows
        public Color Hairline;   // borders
        public Color LineSoft;   // grid lines
        public Color Ink;        // primary text
        public Color Second;     // secondary text
        public Color Muted;      // captions, legends
        public Color Well;       // status strip, grid header, selection
        public Color Solid;      // ink fill (Apply)
        public Color OnSolid;    // text on that fill
        public Color Wash;       // 10% signal hover
        public Color WashPress;

        public static readonly DwPalette Light = new DwPalette
        {
            IsDark = false,
            Chrome = Rgb(0xFFFFFF),
            Paper = Rgb(0xF2F4F1),
            Surface = Rgb(0xFFFFFF),
            Hairline = Rgb(0xDDE0DC),
            LineSoft = Rgb(0xE4E7E3),
            Ink = Rgb(0x0F1A17),
            Second = Rgb(0x4A4E4B),
            Muted = Rgb(0x6B6F6C),
            Well = Rgb(0xE4E7E3),
            Solid = Rgb(0x0F1A17),
            OnSolid = Rgb(0xF2F4F1),
            Wash = Rgb(0xE9EEFC),
            WashPress = Rgb(0xDCE4FA)
        };

        // The design's dark tokens are paper at low alpha over ink; WinForms controls
        // don't composite, so each value is pre-flattened against its own surface.
        public static readonly DwPalette Dark = new DwPalette
        {
            IsDark = true,
            Chrome = Rgb(0x283330),
            Paper = Color.FromArgb(25, 36, 33),
            Surface = Color.FromArgb(34, 44, 41),
            Hairline = Color.FromArgb(58, 67, 64),
            LineSoft = Color.FromArgb(51, 60, 57),
            Ink = Rgb(0xF2F4F1),
            Second = Color.FromArgb(181, 186, 183),
            Muted = Color.FromArgb(123, 130, 127),
            Well = Color.FromArgb(40, 51, 48),
            Solid = Rgb(0xF2F4F1),
            OnSolid = Rgb(0x0F1A17),
            Wash = Color.FromArgb(45, 61, 74),
            WashPress = Color.FromArgb(52, 71, 89)
        };

        private static Color Rgb(int hex)
        {
            return Color.FromArgb((hex >> 16) & 0xFF, (hex >> 8) & 0xFF, hex & 0xFF);
        }
    }

    /// <summary>What a control is, so it can be recoloured when the theme changes.</summary>
    internal enum DwRole
    {
        None, QuietButton, SignalButton, InkButton,
        TextInk, TextSecond, TextMuted,
        Group, Check, Field, StatusPanel, Track, Progress, Grid
    }

    internal static class Dw
    {
        public static DwPalette P = DwPalette.Light;

        /// <summary>Raised after the palette changes so open windows can restyle.</summary>
        public static event Action ThemeChanged;

        public static bool IsDark { get { return P.IsDark; } }

        public static void SetTheme(bool dark)
        {
            var next = dark ? DwPalette.Dark : DwPalette.Light;
            if (next == P) return;
            P = next;
            Settings.WriteDark(dark);
            var handler = ThemeChanged;
            if (handler != null) handler();
        }

        // Role colours, read live so a theme switch is picked up everywhere.
        public static Color Paper { get { return P.Paper; } }
        public static Color Surface { get { return P.Surface; } }
        public static Color Chrome { get { return P.Chrome; } }
        public static Color Hairline { get { return P.Hairline; } }
        public static Color LineSoft { get { return P.LineSoft; } }
        public static Color Ink { get { return P.Ink; } }
        public static Color Second { get { return P.Second; } }
        public static Color Muted { get { return P.Muted; } }
        public static Color Well { get { return P.Well; } }
        public static Color Solid { get { return P.Solid; } }
        public static Color OnSolid { get { return P.OnSolid; } }
        public static Color SignalWash { get { return P.Wash; } }
        public static Color SignalWashPress { get { return P.WashPress; } }

        // Signal and error are fixed in both themes — the system's closed set.
        public static readonly Color Signal = Color.FromArgb(0x2B, 0x5C, 0xE6);
        public static readonly Color SignalHover = Color.FromArgb(0x24, 0x50, 0xC9);
        public static readonly Color SignalPress = Color.FromArgb(0x1F, 0x45, 0xB4);
        public static readonly Color Error = Color.FromArgb(0xC4, 0x45, 0x3A);
        public static readonly Color White = Color.White;

        // Disabled is 40% opacity; a flat button keeps painting its BackColor when
        // disabled, so the muted pair has to be applied explicitly.
        public static Color SignalMuted
        {
            get { return P.IsDark ? Color.FromArgb(0x2A, 0x3E, 0x76) : Color.FromArgb(0xAF, 0xC1, 0xF2); }
        }
        public static Color OnSignalMuted
        {
            get { return P.IsDark ? Color.FromArgb(0x8E, 0x9B, 0xB8) : Color.FromArgb(0xE6, 0xEC, 0xFB); }
        }

        #region Fonts

        // Point sizes, so WinForms' own DPI scaling applies and GDI keeps hinting them.
        public static readonly Font Ui = new Font("Segoe UI", 9f);
        public static readonly Font UiBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font UiSmall = new Font("Segoe UI", 8.25f);
        public static readonly Font Mono = new Font("Consolas", 8.25f);
        public static readonly Font MonoBold = new Font("Consolas", 8.25f, FontStyle.Bold);
        public static readonly Font Legend = new Font("Segoe UI", 7.5f, FontStyle.Bold);

        #endregion

        #region Builders

        /// <summary>Quiet control: surface with a hairline, signal wash on hover.</summary>
        public static Button QuietButton(string text)
        {
            var b = BaseButton(text, DwRole.QuietButton);
            StyleQuiet(b);
            return b;
        }

        /// <summary>The one signal element on a surface.</summary>
        public static Button SignalButton(string text)
        {
            var b = BaseButton(text, DwRole.SignalButton);
            StyleSignal(b);
            return b;
        }

        /// <summary>Ink fill: confirms without competing with the signal.</summary>
        public static Button InkButton(string text)
        {
            var b = BaseButton(text, DwRole.InkButton);
            StyleInk(b);
            return b;
        }

        private static Button BaseButton(string text, DwRole role)
        {
            var b = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = Ui,
                UseVisualStyleBackColor = false,
                Height = 26,
                Tag = role
            };
            b.FlatAppearance.BorderSize = 1;
            return b;
        }

        private static void StyleQuiet(Button b)
        {
            b.BackColor = Surface;
            b.ForeColor = Ink;
            b.FlatAppearance.BorderColor = Hairline;
            b.FlatAppearance.MouseOverBackColor = SignalWash;
            b.FlatAppearance.MouseDownBackColor = SignalWashPress;
        }

        private static void StyleSignal(Button b)
        {
            b.BackColor = b.Enabled ? Signal : SignalMuted;
            b.ForeColor = b.Enabled ? White : OnSignalMuted;
            b.FlatAppearance.BorderColor = b.BackColor;
            b.FlatAppearance.MouseOverBackColor = SignalHover;
            b.FlatAppearance.MouseDownBackColor = SignalPress;
        }

        private static void StyleInk(Button b)
        {
            b.BackColor = Solid;
            b.ForeColor = OnSolid;
            b.FlatAppearance.BorderColor = Solid;
            b.FlatAppearance.MouseOverBackColor = Second;
            b.FlatAppearance.MouseDownBackColor = Solid;
        }

        public static Label Text(string text, int x, int y, int width, Color color, Font font)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 16),
                ForeColor = color,
                Font = font,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Tag = color == Ink ? DwRole.TextInk : color == Second ? DwRole.TextSecond : DwRole.TextMuted
            };
        }

        /// <summary>
        /// A group box with an uppercase legend. Rendering is left entirely to the
        /// control — drawing over it doubles the caption and strikes it through.
        /// </summary>
        public static GroupBox Group(string legend, int x, int y, int width, int height)
        {
            return new GroupBox
            {
                Text = legend.ToUpperInvariant(),
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = Legend,
                ForeColor = Muted,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Tag = DwRole.Group
            };
        }

        public static CheckBox Check(string text, int x, int y, int width, bool value)
        {
            var c = new CheckBox
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 22),
                Checked = value,
                Font = Ui,
                ForeColor = Ink,
                BackColor = Color.Transparent,
                Tag = DwRole.Check
            };
            StyleCheck(c);
            return c;
        }

        private static void StyleCheck(CheckBox c)
        {
            c.ForeColor = Ink;
            if (P.IsDark)
            {
                // The themed glyph is a white box that can't be recoloured. Flat fills
                // the box from BackColor, so it has to be opaque, not Transparent.
                c.FlatStyle = FlatStyle.Flat;
                c.BackColor = Paper;
                c.FlatAppearance.BorderColor = Hairline;
                c.FlatAppearance.CheckedBackColor = Signal;
            }
            else
            {
                c.FlatStyle = FlatStyle.Standard;
                c.BackColor = Color.Transparent;
            }
        }

        /// <summary>
        /// NumericUpDown hosts its own edit box, and that child keeps the old colour
        /// unless it is set directly.
        /// </summary>
        public static void StyleField(Control c)
        {
            c.BackColor = Surface;
            c.ForeColor = Ink;
            foreach (Control child in c.Controls)
            {
                child.BackColor = Surface;
                child.ForeColor = Ink;
            }
        }

        public static void StyleGrid(DataGridView g)
        {
            g.Tag = DwRole.Grid;
            g.BackgroundColor = Surface;
            g.GridColor = LineSoft;
            g.ColumnHeadersDefaultCellStyle.BackColor = Well;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Well;
            g.DefaultCellStyle.BackColor = Surface;
            g.DefaultCellStyle.ForeColor = Second;
            g.DefaultCellStyle.SelectionBackColor = Well;
            g.DefaultCellStyle.SelectionForeColor = Ink;

            foreach (DataGridViewColumn col in g.Columns)
            {
                string name = col.Name;
                if (name == "act" || name == "wait") col.DefaultCellStyle.ForeColor = Ink;
                else if (name != "idx") col.DefaultCellStyle.ForeColor = Second;
            }
        }

        #endregion

        /// <summary>Re-applies the current palette to a control built by the helpers above.</summary>
        public static void Restyle(Control c)
        {
            if (!(c.Tag is DwRole)) return;
            switch ((DwRole)c.Tag)
            {
                case DwRole.QuietButton: StyleQuiet((Button)c); break;
                case DwRole.SignalButton: StyleSignal((Button)c); break;
                case DwRole.InkButton: StyleInk((Button)c); break;
                case DwRole.TextInk: c.ForeColor = Ink; break;
                case DwRole.TextSecond: c.ForeColor = Second; break;
                case DwRole.TextMuted: c.ForeColor = Muted; break;
                case DwRole.Group: c.ForeColor = Muted; break;
                case DwRole.Check: StyleCheck((CheckBox)c); break;
                case DwRole.Field: StyleField(c); break;
                case DwRole.StatusPanel: c.BackColor = Well; break;
                case DwRole.Track: c.BackColor = Hairline; break;
                case DwRole.Progress: break; // colour is owned by the run state
                case DwRole.Grid: StyleGrid((DataGridView)c); break;
            }
        }

        public static void RestyleTree(Control root)
        {
            foreach (Control c in root.Controls)
            {
                Restyle(c);
                RestyleTree(c);
            }
        }
    }

    /// <summary>Remembers the chosen theme under HKCU so it survives a restart.</summary>
    internal static class Settings
    {
        private const string KeyPath = @"Software\DOLFWA\Dolftasker2";

        public static bool? ReadDark()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(KeyPath))
                {
                    if (key == null) return null;
                    string value = key.GetValue("Theme") as string;
                    if (value == "dark") return true;
                    if (value == "light") return false;
                    return null;
                }
            }
            catch { return null; }
        }

        public static void WriteDark(bool dark)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(KeyPath))
                    if (key != null) key.SetValue("Theme", dark ? "dark" : "light");
            }
            catch { /* a read-only hive shouldn't stop the app */ }
        }

        /// <summary>Falls back to the Windows app-theme setting the first time DolfTask runs.</summary>
        public static bool OsPrefersDark()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key == null) return false;
                    object value = key.GetValue("AppsUseLightTheme");
                    return value is int && (int)value == 0;
                }
            }
            catch { return false; }
        }
    }

    /// <summary>
    /// Borderless form with the design's own 30px title bar: mark, window name,
    /// an optional theme toggle, and flat minimise / close glyphs.
    /// </summary>
    internal class DtWindow : Form
    {
        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 1, HTCAPTION = 2;
        private const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;
        private const int CS_DROPSHADOW = 0x20000;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public string WindowTitle = "Dolftasker.2";
        public bool ShowMinimise = true;
        public bool ShowThemeToggle;
        public bool Resizable;

        private int _hotButton = -1; // 0 minimise, 1 close, 2 theme

        protected const int TitleBarHeight = 30;
        private const int ButtonSize = 26;
        private const int GripSize = 6;

        public DtWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Dw.Paper;
            Font = Dw.Ui;
            AutoScaleMode = AutoScaleMode.Font;
            DoubleBuffered = true;
            Dw.ThemeChanged += OnThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Dw.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        protected virtual void OnThemeChanged()
        {
            BackColor = Dw.Paper;
            Dw.RestyleTree(this);
            Invalidate(true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int pref = 3; // DWMWCP_ROUNDSMALL
                DwmSetWindowAttribute(Handle, 33, ref pref, sizeof(int));
            }
            catch { /* pre-Win11 */ }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;

            using (var brush = new SolidBrush(Dw.Chrome))
                g.FillRectangle(brush, 0, 0, Width, TitleBarHeight);
            using (var pen = new Pen(Dw.Hairline))
            {
                g.DrawLine(pen, 0, TitleBarHeight - 1, Width, TitleBarHeight - 1);
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }

            DrawMark(g, 10, (TitleBarHeight - 16) / 2, 16);
            TextRenderer.DrawText(g, WindowTitle, Dw.UiBold,
                new Rectangle(33, 0, Width - 120, TitleBarHeight),
                Dw.Ink, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            if (ShowThemeToggle) DrawThemeGlyph(g, ThemeRect, _hotButton == 2);
            if (ShowMinimise) DrawTitleButton(g, "–", MinimiseRect, _hotButton == 0);
            DrawTitleButton(g, "×", CloseRect, _hotButton == 1);
        }

        private void DrawTitleButton(Graphics g, string glyph, Rectangle rect, bool hot)
        {
            if (hot)
                using (var brush = new SolidBrush(Dw.Well))
                    g.FillRectangle(brush, rect);
            TextRenderer.DrawText(g, glyph, Dw.Ui, rect, Dw.Second,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // A half-filled circle: the standard "switch appearance" mark, and it needs
        // no font that might not have the glyph.
        private void DrawThemeGlyph(Graphics g, Rectangle rect, bool hot)
        {
            if (hot)
                using (var brush = new SolidBrush(Dw.Well))
                    g.FillRectangle(brush, rect);

            const int d = 12;
            var circle = new Rectangle(rect.X + (rect.Width - d) / 2, rect.Y + (rect.Height - d) / 2, d, d);
            var old = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var pen = new Pen(Dw.Second))
                g.DrawEllipse(pen, circle);
            using (var brush = new SolidBrush(Dw.Second))
                g.FillPie(brush, circle, 90, 180);
            g.SmoothingMode = old;
        }

        /// <summary>The DOLFWA mark, drawn as geometry — the counter is knocked out in the surface colour.</summary>
        private static void DrawMark(Graphics g, int x, int y, int height)
        {
            float s = height / 16f;
            var old = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var outer = RoundPath(x, y, 13 * s, 16 * s, 1.5f * s, 8 * s))
            using (var brush = new SolidBrush(Dw.Ink))
                g.FillPath(brush, outer);
            using (var inner = RoundPath(x + 3.9f * s, y + 4 * s, 7.4f * s, 8 * s, 0.5f * s, 4 * s))
            using (var brush = new SolidBrush(Dw.Chrome))
                g.FillPath(brush, inner);
            g.SmoothingMode = old;
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundPath(float x, float y, float w, float h, float left, float right)
        {
            var p = new System.Drawing.Drawing2D.GraphicsPath();
            p.AddArc(x, y, left * 2, left * 2, 180, 90);
            p.AddArc(x + w - right * 2, y, right * 2, right * 2, 270, 90);
            p.AddArc(x + w - right * 2, y + h - right * 2, right * 2, right * 2, 0, 90);
            p.AddArc(x, y + h - left * 2, left * 2, left * 2, 90, 90);
            p.CloseFigure();
            return p;
        }

        private Rectangle CloseRect
        {
            get { return new Rectangle(Width - ButtonSize - 4, (TitleBarHeight - ButtonSize) / 2, ButtonSize, ButtonSize); }
        }

        private Rectangle MinimiseRect
        {
            get { var c = CloseRect; return new Rectangle(c.X - ButtonSize - 2, c.Y, ButtonSize, ButtonSize); }
        }

        private Rectangle ThemeRect
        {
            get
            {
                var r = ShowMinimise ? MinimiseRect : CloseRect;
                return new Rectangle(r.X - ButtonSize - 2, r.Y, ButtonSize, ButtonSize);
            }
        }

        private int HitTitleButton(Point p)
        {
            if (CloseRect.Contains(p)) return 1;
            if (ShowMinimise && MinimiseRect.Contains(p)) return 0;
            if (ShowThemeToggle && ThemeRect.Contains(p)) return 2;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int hot = HitTitleButton(e.Location);
            if (hot != _hotButton) { _hotButton = hot; Invalidate(new Rectangle(0, 0, Width, TitleBarHeight)); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hotButton != -1) { _hotButton = -1; Invalidate(new Rectangle(0, 0, Width, TitleBarHeight)); }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                switch (HitTitleButton(e.Location))
                {
                    case 1: Close(); return;
                    case 0: WindowState = FormWindowState.Minimized; return;
                    case 2: Dw.SetTheme(!Dw.IsDark); return;
                }
            }
            base.OnMouseDown(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if (m.Result.ToInt32() != HTCLIENT) return;

                var p = PointToClient(new Point(m.LParam.ToInt32() & 0xFFFF, (m.LParam.ToInt32() >> 16) & 0xFFFF));

                if (Resizable)
                {
                    bool left = p.X <= GripSize, right = p.X >= Width - GripSize;
                    bool top = p.Y <= GripSize, bottom = p.Y >= Height - GripSize;
                    if (bottom && right) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
                    if (bottom && left) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
                    if (top && right) { m.Result = (IntPtr)HTTOPRIGHT; return; }
                    if (top && left) { m.Result = (IntPtr)HTTOPLEFT; return; }
                    if (bottom) { m.Result = (IntPtr)HTBOTTOM; return; }
                    if (top) { m.Result = (IntPtr)HTTOP; return; }
                    if (left) { m.Result = (IntPtr)HTLEFT; return; }
                    if (right) { m.Result = (IntPtr)HTRIGHT; return; }
                }

                if (p.Y < TitleBarHeight && HitTitleButton(p) < 0)
                    m.Result = (IntPtr)HTCAPTION;
                return;
            }

            base.WndProc(ref m);
        }
    }
}
