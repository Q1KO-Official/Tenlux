using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

public class DarkModeTrayApp
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint Msg, IntPtr wParam, string lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint WM_SETTINGCHANGE = 0x001A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private static NotifyIcon _notifyIcon;
    private static OverlayForm _overlayForm;
    private static System.Threading.Mutex _appMutex;
    private static bool _singleClickToggle = true;
    private static bool _disableOverlay = false;
    private static int _overlaySpeedLevel = 2;
    private static int _lang = 0;
    private const string RegPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string StartupRegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "ToggleDarkMode";

    private static readonly string[][] _loc = new string[][] {
        new[] { "\ud83c\udf10\u8bed\u8a00", "\ud83c\udf10Language", "\ud83c\udf10\u8a9e\u8a00" },
        new[] { "English", "English", "English" },
        new[] { "\u4e2d\u6587\uff08\u7b80\u4f53\uff09", "\u4e2d\u6587\uff08\u7b80\u4f53\uff09", "\u4e2d\u6587\uff08\u7c21\u9ad4\uff09" },
        new[] { "\u4e2d\u6587\uff08\u7e41\u4f53\uff09", "\u4e2d\u6587\uff08\u7e41\u4f53\uff09", "\u4e2d\u6587\uff08\u7e41\u9ad4\uff09" },
        new[] { "\u5207\u6362\u6a21\u5f0f", "Switch Mode", "\u5207\u63db\u6a21\u5f0f" },
        new[] { "\u5355\u51fb\u5207\u6362", "Single Click", "\u55ae\u64ca\u5207\u63db" },
        new[] { "\u53cc\u51fb\u5207\u6362", "Double Click", "\u96d9\u64ca\u5207\u63db" },
        new[] { "\u5173\u95ed\u906e\u7f69", "Disable Overlay", "\u95dc\u9589\u906e\u7f69" },
        new[] { "\u6253\u5f00\u906e\u7f69", "Enable Overlay", "\u6253\u958b\u906e\u7f69" },
        new[] { "\u906e\u7f69\u6301\u7eed\u65f6\u95f4", "Overlay Duration", "\u906e\u7f69\u6301\u7e8c\u6642\u9593" },
        new[] { "\u957f", "Long", "\u9577" },
        new[] { "\u8f83\u957f", "Semi-Long", "\u8f03\u9577" },
        new[] { "\u9ed8\u8ba4", "Default", "\u9ed8\u8a8d" },
        new[] { "\u8f83\u77ed", "Semi-Short", "\u8f03\u77ed" },
        new[] { "\u77ed", "Short", "\u77ed" },
        new[] { "\u9000\u51fa", "Exit", "\u9000\u51fa" },
        new[] { "\u6df1\u8272/\u6d45\u8272\u6a21\u5f0f\u4e00\u952e\u5207\u6362", "Dark/Light Mode Toggle", "\u6df1\u8272/\u6dfa\u8272\u6a21\u5f0f\u4e00\u9375\u5207\u63db" },
        new[] { "\u5df2\u5207\u6362\u81f3 ", "Switched to ", "\u5df2\u5207\u63db\u81f3 " },
        new[] { "\u6df1\u8272\u6a21\u5f0f", "Dark Mode", "\u6df1\u8272\u6a21\u5f0f" },
        new[] { "\u6d45\u8272\u6a21\u5f0f", "Light Mode", "\u6dfa\u8272\u6a21\u5f0f" },
        new[] { "ToggleDarkMode \u5df2\u7ecf\u5728\u8fd0\u884c\u4e86\uff0c\u8bf7\u67e5\u770b\u4efb\u52a1\u680f\u56fe\u6807\u3002",
                "ToggleDarkMode is already running. Check the system tray.",
                "ToggleDarkMode \u5df2\u7ecf\u5728\u904b\u884c\u4e86\uff0c\u8acb\u67e5\u770b\u4efb\u52d9\u6b04\u5716\u793a\u3002" },
        new[] { "\u5f00\u673a\u81ea\u52a8\u542f\u52a8", "Start with Windows", "\u958b\u6a5f\u81ea\u52d5\u555f\u52d5" },
    };

    private static string L(int i) { return _loc[i][_lang]; }

    private static string ConfigPath
    {
        get
        {
            string docDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(docDir, "ToggleDarkMode", "ToggleDarkMode.cfg");
        }
    }

    private static string LegacyConfigPath
    {
        get
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(exeDir, "ToggleDarkMode.cfg");
        }
    }

    private static void EnsureConfigDirectory()
    {
        try
        {
            string dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        catch { }
    }

    private static void MigrateLegacyConfigIfNeeded()
    {
        try
        {
            if (File.Exists(ConfigPath)) return;
            if (!File.Exists(LegacyConfigPath)) return;
            EnsureConfigDirectory();
            File.Copy(LegacyConfigPath, ConfigPath, false);
        }
        catch { }
    }

    private static void LoadSettings()
    {
        try
        {
            MigrateLegacyConfigIfNeeded();
            string path = ConfigPath;
            if (!File.Exists(path)) return;
            foreach (string line in File.ReadAllLines(path, System.Text.Encoding.UTF8))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                int eq = trimmed.IndexOf('=');
                if (eq < 0) continue;
                string key = trimmed.Substring(0, eq).Trim();
                string val = trimmed.Substring(eq + 1).Trim();
                int iv;
                if (int.TryParse(val, out iv))
                {
                    if (key == "SingleClickToggle") _singleClickToggle = (iv != 0);
                    else if (key == "DisableOverlay") _disableOverlay = (iv != 0);
                    else if (key == "SpeedLevel" && iv >= 0 && iv <= 4) _overlaySpeedLevel = iv;
                    else if (key == "Language" && iv >= 0 && iv <= 2) _lang = iv;
                }
            }
        }
        catch { }
    }

    private static void SaveSettings()
    {
        try
        {
            EnsureConfigDirectory();
            string content = "SingleClickToggle=" + (_singleClickToggle ? "1" : "0") + "\n"
                + "DisableOverlay=" + (_disableOverlay ? "1" : "0") + "\n"
                + "SpeedLevel=" + _overlaySpeedLevel + "\n"
                + "Language=" + _lang + "\n";
            File.WriteAllText(ConfigPath, content, System.Text.Encoding.UTF8);
        }
        catch { }
    }

    private static bool IsStartupEnabled()
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(StartupRegPath))
            {
                if (key == null) return false;
                return key.GetValue(StartupValueName) != null;
            }
        }
        catch { }
        return false;
    }

    private static void SetStartupEnabled(bool enabled)
    {
        try
        {
            using (var key = Registry.CurrentUser.CreateSubKey(StartupRegPath))
            {
                if (key == null) return;
                if (enabled)
                {
                    key.SetValue(StartupValueName, "\"" + Application.ExecutablePath + "\"", RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue(StartupValueName, false);
                }
            }
        }
        catch { }
    }

    private static void SetLanguage(int lang, ToolStripMenuItem active, ToolStripMenuItem other1, ToolStripMenuItem other2, System.Action refresh)
    {
        _lang = lang;
        active.Checked = true;
        other1.Checked = false;
        other2.Checked = false;
        refresh();
        SaveSettings();
    }

    [STAThread]
    public static void Main()
    {
        const string mutexName = "DarkModeTrayToggle_SOLO_Unique";

        bool createdNew;
        _appMutex = new System.Threading.Mutex(true, mutexName, out createdNew);

        if (!createdNew)
        {
            MessageBox.Show(L(20),
                "ToggleDarkMode",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _appMutex.Dispose();
            return;
        }

        LoadSettings();

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        bool isLight = ReadCurrentTheme();

        _notifyIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(isLight),
            Text = L(16)
        };

        _notifyIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left && _singleClickToggle)
                DoToggle();
        };

        _notifyIcon.MouseDoubleClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left && !_singleClickToggle)
                DoToggle();
        };

        var singleClickItem = new ToolStripMenuItem(L(5)) { Checked = _singleClickToggle };
        var doubleClickItem = new ToolStripMenuItem(L(6)) { Checked = !_singleClickToggle };

        singleClickItem.Click += (s, e) =>
        {
            _singleClickToggle = true;
            singleClickItem.Checked = true;
            doubleClickItem.Checked = false;
            SaveSettings();
        };

        doubleClickItem.Click += (s, e) =>
        {
            _singleClickToggle = false;
            singleClickItem.Checked = false;
            doubleClickItem.Checked = true;
            SaveSettings();
        };

        var switchModeSubMenu = new ToolStripMenuItem(L(4));
        switchModeSubMenu.DropDownItems.Add(singleClickItem);
        switchModeSubMenu.DropDownItems.Add(doubleClickItem);

        var disableOverlayItem = new ToolStripMenuItem(_disableOverlay ? L(8) : L(7));
        var startupItem = new ToolStripMenuItem(L(21))
        {
            Checked = IsStartupEnabled(),
            CheckOnClick = true
        };

        startupItem.Click += (s, e) =>
        {
            SetStartupEnabled(startupItem.Checked);
            startupItem.Checked = IsStartupEnabled();
        };

        var speedSubMenu = new ToolStripMenuItem(L(9)) { Visible = !_disableOverlay };
        ToolStripMenuItem[] speedItems = new ToolStripMenuItem[5];
        for (int i = 0; i < 5; i++)
        {
            int idx = i;
            speedItems[i] = new ToolStripMenuItem(L(10 + i)) { Checked = (idx == _overlaySpeedLevel) };
            speedItems[i].Click += (s, e) =>
            {
                _overlaySpeedLevel = idx;
                for (int j = 0; j < 5; j++)
                    speedItems[j].Checked = (j == idx);
                SaveSettings();
            };
            speedSubMenu.DropDownItems.Add(speedItems[i]);
        }

        var langMenu = new ToolStripMenuItem(L(0));
        var langItemEn = new ToolStripMenuItem(L(1)) { Checked = (_lang == 1) };
        var langItemZhCn = new ToolStripMenuItem(L(2)) { Checked = (_lang == 0) };
        var langItemZhTw = new ToolStripMenuItem(L(3)) { Checked = (_lang == 2) };
        langMenu.DropDownItems.Add(langItemEn);
        langMenu.DropDownItems.Add(langItemZhCn);
        langMenu.DropDownItems.Add(langItemZhTw);

        var exitItem = new ToolStripMenuItem(L(15));
        exitItem.Click += (s, e) =>
        {
            SaveSettings();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Application.Exit();
        };

        System.Action refreshAll = () =>
        {
            _notifyIcon.Text = L(16);
            langMenu.Text = L(0);
            langItemEn.Text = L(1);
            langItemZhCn.Text = L(2);
            langItemZhTw.Text = L(3);
            switchModeSubMenu.Text = L(4);
            singleClickItem.Text = L(5);
            doubleClickItem.Text = L(6);
            disableOverlayItem.Text = _disableOverlay ? L(8) : L(7);
            startupItem.Text = L(21);
            speedSubMenu.Text = L(9);
            for (int i = 0; i < 5; i++) speedItems[i].Text = L(10 + i);
            exitItem.Text = L(15);
        };

        disableOverlayItem.Click += (s, e) =>
        {
            _disableOverlay = !_disableOverlay;
            disableOverlayItem.Text = _disableOverlay ? L(8) : L(7);
            speedSubMenu.Visible = !_disableOverlay;
            SaveSettings();
        };

        langItemEn.Click += (s, e) => SetLanguage(1, langItemEn, langItemZhCn, langItemZhTw, refreshAll);
        langItemZhCn.Click += (s, e) => SetLanguage(0, langItemZhCn, langItemEn, langItemZhTw, refreshAll);
        langItemZhTw.Click += (s, e) => SetLanguage(2, langItemZhTw, langItemEn, langItemZhCn, refreshAll);

        var noBorderRenderer = new NoBorderRenderer();

        System.EventHandler subMenuOpening = (s, e) =>
        {
            ((ToolStripMenuItem)s).DropDownDirection = ToolStripDropDownDirection.Right;
        };

        langMenu.DropDown.Renderer = noBorderRenderer;
        langMenu.DropDownOpening += subMenuOpening;

        switchModeSubMenu.DropDown.Renderer = noBorderRenderer;
        switchModeSubMenu.DropDownOpening += subMenuOpening;

        speedSubMenu.DropDown.Renderer = noBorderRenderer;
        speedSubMenu.DropDownOpening += subMenuOpening;

        var ctxMenu = new ContextMenuStrip();
        ctxMenu.RightToLeft = RightToLeft.No;
        ctxMenu.Opening += (s, e) =>
        {
            startupItem.Checked = IsStartupEnabled();
        };
        ctxMenu.Items.Add(langMenu);
        ctxMenu.Items.Add(switchModeSubMenu);
        ctxMenu.Items.Add(startupItem);
        ctxMenu.Items.Add(disableOverlayItem);
        ctxMenu.Items.Add(speedSubMenu);
        ctxMenu.Items.Add(new ToolStripSeparator());
        ctxMenu.Items.Add(exitItem);
        _notifyIcon.ContextMenuStrip = ctxMenu;

        _notifyIcon.Visible = true;

        Application.Run();
    }

    private static bool ReadCurrentTheme()
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegPath))
            {
                if (key != null)
                {
                    var val = key.GetValue("SystemUsesLightTheme");
                    if (val is int) return ((int)val == 1);
                }
            }
        }
        catch { }
        return true;
    }

    private static void ApplyRegistryToggle(int newValue)
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegPath, true))
            {
                if (key != null)
                {
                    key.SetValue("SystemUsesLightTheme", newValue, RegistryValueKind.DWord);
                    key.SetValue("AppsUseLightTheme", newValue, RegistryValueKind.DWord);
                }
            }
        }
        catch { }

        IntPtr result;
        SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "ImmersiveColorSet", SMTO_ABORTIFHUNG, 5000, out result);
    }

    private static int GetSpeedDurationMs()
    {
        int[] durations = { 1000, 800, 600, 400, 200 };
        return durations[_overlaySpeedLevel];
    }

    private static void DoToggle()
    {
        if (_overlayForm != null && !_overlayForm.IsDisposed) return;

        bool isLight = ReadCurrentTheme();
        int newValue = isLight ? 0 : 1;

        _notifyIcon.Icon = CreateTrayIcon(!isLight);

        if (_disableOverlay)
        {
            ApplyRegistryToggle(newValue);
        }
        else
        {
            string modeName = isLight ? L(18) : L(19);
            _overlayForm = new OverlayForm(L(17) + modeName, newValue, GetSpeedDurationMs(), () => _overlayForm = null);
            _overlayForm.Show();
        }
    }

    private static Icon CreateTrayIcon(bool isLight)
    {
        int size = 32;
        var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            int m = 3;
            int cs = size - m * 2;

            if (isLight)
            {
                using (var brush = new SolidBrush(Color.FromArgb(255, 200, 40)))
                    g.FillEllipse(brush, m, m, cs, cs);
                using (var pen = new Pen(Color.FromArgb(255, 160, 20), 1.5f))
                    g.DrawEllipse(pen, m, m, cs, cs);
            }
            else
            {
                using (var brush = new SolidBrush(Color.FromArgb(50, 50, 60)))
                    g.FillEllipse(brush, m, m, cs, cs);
                using (var pen = new Pen(Color.FromArgb(160, 160, 170), 2f))
                    g.DrawEllipse(pen, m, m, cs, cs);
            }
        }

        IntPtr hIcon = bmp.GetHicon();
        var result = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        bmp.Dispose();
        return result;
    }

    private class OverlayForm : Form
    {
        private Timer _timer;
        private readonly bool _isLightTarget;
        private readonly TableLayoutPanel _centerPanel;
        private readonly IconControl _iconCtrl;
        private readonly Label _mainLabel;
        private readonly Label _subLabel;
        private readonly int _newValue;
        private readonly Action _onClosed;
        private int _phase;
        private int _tickCount;

        private readonly int FadeInTicks;
        private readonly int HoldTicks;
        private readonly int FadeOutTicks;
        private const int TimerInterval = 16;

        public OverlayForm(string fullText, int newValue, int totalDurationMs, Action onClosed)
        {
            _newValue = newValue;
            _onClosed = onClosed;
            _isLightTarget = (newValue == 1);

            double totalTicks = (double)totalDurationMs / TimerInterval;
            FadeInTicks = Math.Max(1, (int)Math.Round(totalTicks * 0.42));
            HoldTicks = Math.Max(1, (int)Math.Round(totalTicks * 0.23));
            int fadeOutTicks = Math.Max(1, (int)Math.Round(totalTicks * 0.35));
            int sum = FadeInTicks + HoldTicks + fadeOutTicks;
            if (sum < totalTicks) fadeOutTicks += (int)(totalTicks - sum);
            FadeOutTicks = fadeOutTicks;

            Color bgColor = _isLightTarget ? Color.White : Color.Black;
            Color textColor = _isLightTarget ? Color.Black : Color.White;
            Color subColor = _isLightTarget
                ? Color.FromArgb(170, 60, 60, 60)
                : Color.FromArgb(170, 255, 255, 255);

            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = Screen.PrimaryScreen.Bounds;
            BackColor = bgColor;
            Opacity = 0.0;
            DoubleBuffered = true;

            _centerPanel = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 3,
                Anchor = AnchorStyles.None,
                BackColor = bgColor
            };
            _centerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _centerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _centerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _centerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _iconCtrl = new IconControl(_isLightTarget, bgColor)
            {
                Size = new Size(120, 120),
                Margin = new Padding(0, 0, 0, 12)
            };
            _centerPanel.Controls.Add(_iconCtrl, 0, 0);

            _mainLabel = new Label
            {
                Text = fullText,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 28, FontStyle.Regular),
                ForeColor = textColor,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4)
            };
            _centerPanel.Controls.Add(_mainLabel, 0, 1);

            _subLabel = new Label
            {
                Text = "ToggleDarkMode",
                TextAlign = ContentAlignment.TopCenter,
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 16, FontStyle.Regular),
                ForeColor = subColor,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 0)
            };
            _centerPanel.Controls.Add(_subLabel, 0, 2);

            Controls.Add(_centerPanel);

            _phase = 0;
            _tickCount = 0;

            _timer = new Timer { Interval = TimerInterval };
            _timer.Tick += OnTimerTick;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            int pW = _centerPanel.PreferredSize.Width;
            int pH = _centerPanel.PreferredSize.Height;
            _centerPanel.Location = new Point(
                Bounds.Width - pW - 60,
                Bounds.Height - pH - 80
            );
            _timer.Start();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            switch (_phase)
            {
                case 0:
                    _tickCount++;
                    double p = (double)_tickCount / FadeInTicks;
                    if (_tickCount >= FadeInTicks)
                    {
                        Opacity = 1.0;
                        ApplyRegistryToggle(_newValue);
                        _phase = 1;
                        _tickCount = 0;
                    }
                    else
                    {
                        Opacity = EaseOutCubic(p);
                    }
                    break;

                case 1:
                    _tickCount++;
                    if (_tickCount >= HoldTicks)
                    {
                        _phase = 2;
                        _tickCount = 0;
                    }
                    break;

                case 2:
                    _tickCount++;
                    double r = 1.0 - (double)_tickCount / FadeOutTicks;
                    if (_tickCount >= FadeOutTicks || r <= 0.0)
                    {
                        Opacity = 0.0;
                        _timer.Stop();
                        _onClosed();
                        Close();
                        Dispose();
                        return;
                    }
                    Opacity = EaseOutCubic(r);
                    break;
            }
        }

        private static double EaseOutCubic(double t)
        {
            return 1.0 - Math.Pow(1.0 - Math.Max(0, Math.Min(1, t)), 3.0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timer != null) _timer.Dispose();
                if (_iconCtrl != null) _iconCtrl.Dispose();
                if (_mainLabel != null) _mainLabel.Dispose();
                if (_subLabel != null) _subLabel.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    private class IconControl : Control
    {
        private readonly bool _isLightTarget;
        private readonly Color _bgColor;

        public IconControl(bool isLightTarget, Color bgColor)
        {
            _isLightTarget = isLightTarget;
            _bgColor = bgColor;
            DoubleBuffered = true;
            BackColor = bgColor;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int size = Math.Min(Width, Height) - 10;
            int cx = Width / 2;
            int cy = Height / 2;

            if (_isLightTarget)
            {
                int r = size / 2;
                int rayCount = 8;
                int rayLen = r / 3;
                Color rayColor = Color.FromArgb(120, 255, 200, 40);

                for (int i = 0; i < rayCount; i++)
                {
                    double angle = i * Math.PI * 2 / rayCount;
                    int x1 = cx + (int)((r + 2) * Math.Cos(angle));
                    int y1 = cy + (int)((r + 2) * Math.Sin(angle));
                    int x2 = cx + (int)((r + rayLen) * Math.Cos(angle));
                    int y2 = cy + (int)((r + rayLen) * Math.Sin(angle));
                    using (var pen = new Pen(rayColor, 3))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        g.DrawLine(pen, x1, y1, x2, y2);
                    }
                }

                int coreR = r * 7 / 10;
                using (var brush = new SolidBrush(Color.FromArgb(255, 220, 50)))
                    g.FillEllipse(brush, cx - coreR, cy - coreR, coreR * 2, coreR * 2);
            }
            else
            {
                int r = size / 2;
                using (var brush = new SolidBrush(Color.FromArgb(180, 200, 255)))
                    g.FillEllipse(brush, cx - r, cy - r, r * 2, r * 2);

                int cutR = r * 3 / 4;
                int offsetX = r / 3;
                using (var brush = new SolidBrush(_bgColor))
                    g.FillEllipse(brush, cx + offsetX - cutR, cy - cutR, cutR * 2, cutR * 2);

                int starSz = 3;
                int sx1 = cx - r * 2 / 3;
                int sy1 = cy - r * 2 / 3;
                int sx2 = cx + r * 3 / 5;
                int sy2 = cy - r / 3;
                int sx3 = cx - r / 5;
                int sy3 = cy + r * 2 / 5;
                g.FillEllipse(Brushes.White, sx1 - starSz / 2, sy1 - starSz / 2, starSz, starSz);
                g.FillEllipse(Brushes.White, sx2 - starSz / 2, sy2 - starSz / 2, starSz, starSz);
                g.FillEllipse(Brushes.White, sx3 - starSz / 2, sy3 - starSz / 2, starSz, starSz);
            }
        }
    }

    private class NoBorderRenderer : ToolStripProfessionalRenderer
    {
        public NoBorderRenderer() : base(new NoBorderColorTable()) { }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
        }
    }

    private class NoBorderColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return SystemColors.Control; } }
        public override Color ImageMarginGradientBegin { get { return SystemColors.Control; } }
        public override Color ImageMarginGradientMiddle { get { return SystemColors.Control; } }
        public override Color ImageMarginGradientEnd { get { return SystemColors.Control; } }
        public override Color MenuBorder { get { return SystemColors.Control; } }
    }
}
