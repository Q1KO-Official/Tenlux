using System.Runtime.InteropServices;
using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using static Tenlux.Helpers.NativeMethods;

namespace Tenlux.Helpers;

internal sealed class TrayIconService : IDisposable
{
    private const int IdToggle = 1;
    private const int IdSettings = 2;
    private const int IdExit = 3;

    private readonly SettingsManager _settings;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Func<bool> _isLight;
    private readonly Action _toggleTheme;
    private readonly Action _showSettings;
    private readonly Action _exit;
    private readonly object _balloonTimerLock = new();
    private readonly List<Timer> _balloonTimers = [];

    private readonly BitmapImage _iconDark;
    private readonly BitmapImage _iconLight;
    private TaskbarIcon? _trayIcon;

    public TrayIconService(
        SettingsManager settings,
        DispatcherQueue dispatcherQueue,
        Func<bool> isLight,
        Action toggleTheme,
        Action showSettings,
        Action exit)
    {
        _settings = settings;
        _dispatcherQueue = dispatcherQueue;
        _isLight = isLight;
        _toggleTheme = toggleTheme;
        _showSettings = showSettings;
        _exit = exit;

        var baseDir = AppContext.BaseDirectory;
        _iconDark = new BitmapImage { DecodePixelWidth = 16, DecodePixelHeight = 16, UriSource = new Uri(Path.Combine(baseDir, "Assets", "dark.ico")) };
        _iconLight = new BitmapImage { DecodePixelWidth = 16, DecodePixelHeight = 16, UriSource = new Uri(Path.Combine(baseDir, "Assets", "light.ico")) };
    }

    public void Create()
    {
        _trayIcon = new TaskbarIcon
        {
            LeftClickCommand = new SimpleCommand(OnLeftClick),
            DoubleClickCommand = new SimpleCommand(OnDoubleClick),
            RightClickCommand = new SimpleCommand(ShowNativeMenu),
        };
        UpdateState();
        _trayIcon.ForceCreate();
    }

    public void UpdateState()
    {
        if (_trayIcon == null) return;
        var isLight = _isLight();
        _trayIcon.IconSource = isLight ? _iconLight : _iconDark;
        _trayIcon.ToolTipText = isLight ? Localizer.T(Localizer.S_LightMode) : Localizer.T(Localizer.S_DarkMode);
    }

    public void Dispose()
    {
        lock (_balloonTimerLock)
        {
            foreach (var timer in _balloonTimers)
                timer.Dispose();
            _balloonTimers.Clear();
        }

        try { _trayIcon?.Dispose(); }
        catch (Exception ex) { AppLogger.Log(ex, "Tray icon dispose failed"); }
        _trayIcon = null;
    }

    public void ShowBalloon(string message)
    {
        try
        {
            var hwnd = CreateWindowEx(0, "Static", "", 0, 0, 0, 0, 0,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = hwnd,
                uID = 1,
                uFlags = NIF_INFO,
                szInfoTitle = "Tenlux",
                szInfo = message,
                dwInfoFlags = NIIF_INFO,
                hIcon = ExtractIconW(IntPtr.Zero, Environment.ProcessPath ?? "", 0),
            };
            Shell_NotifyIcon(NIM_ADD, ref nid);

            Timer? cleanupTimer = null;
            cleanupTimer = new Timer(_ =>
            {
                try
                {
                    Shell_NotifyIcon(NIM_DELETE, ref nid);
                    DestroyWindow(hwnd);
                    if (nid.hIcon != IntPtr.Zero) DestroyIcon(nid.hIcon);
                }
                catch (Exception ex)
                {
                    AppLogger.Log(ex, "Tray balloon cleanup failed");
                }
                finally
                {
                    lock (_balloonTimerLock)
                    {
                        if (cleanupTimer != null)
                            _balloonTimers.Remove(cleanupTimer);
                    }
                    cleanupTimer?.Dispose();
                }
            }, null, 5000, Timeout.Infinite);
            lock (_balloonTimerLock)
                _balloonTimers.Add(cleanupTimer);
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Tray balloon show failed");
        }
    }

    private void ShowNativeMenu()
    {
        SetPreferredAppMode(PreferredAppMode.AllowDark);
        var hMenu = CreatePopupMenu();
        var menuOwner = IntPtr.Zero;
        try
        {
            AppendMenu(hMenu, MF_STRING, IdToggle, Localizer.T(Localizer.S_SwitchMode));
            AppendMenu(hMenu, MF_SEPARATOR, 0, string.Empty);
            AppendMenu(hMenu, MF_STRING, IdSettings, Localizer.T(Localizer.S_Settings));
            AppendMenu(hMenu, MF_STRING, IdExit, Localizer.T(Localizer.S_Exit));

            menuOwner = CreateWindowEx(0, "Static", "", 0, 0, 0, 0, 0,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            GetCursorPos(out var pt);
            SetForegroundWindow(menuOwner);
            var cmd = TrackPopupMenuRet(hMenu, TPM_RIGHTBUTTON | TPM_BOTTOMALIGN | TPM_RETURNCMD,
                pt.X, pt.Y, 0, menuOwner, IntPtr.Zero);
            PostMessage(menuOwner, 0, 0, 0);

            if (cmd == IdToggle) _dispatcherQueue.TryEnqueue(() => _toggleTheme());
            else if (cmd == IdSettings) _dispatcherQueue.TryEnqueue(() => _showSettings());
            else if (cmd == IdExit) _dispatcherQueue.TryEnqueue(() => _exit());
        }
        finally
        {
            DestroyMenu(hMenu);
            if (menuOwner != IntPtr.Zero) DestroyWindow(menuOwner);
        }
    }

    private void OnLeftClick()
    {
        if (!_settings.TrayClickEnabled || !_settings.SingleClickToggle) return;
        _toggleTheme();
    }

    private void OnDoubleClick()
    {
        if (!_settings.TrayClickEnabled) return;
        if (!_settings.SingleClickToggle) _toggleTheme();
    }
}
