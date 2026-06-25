using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Tenlux.Helpers;
using Tenlux.Pages;
using System.Threading;
using static Tenlux.Helpers.NativeMethods;

namespace Tenlux;

public sealed partial class MainWindow : Window
{
    private const int OpeningOverlayFadeDelayMs = 1;
    private const int OpeningOverlayMaxHoldMs = 700;
    private const int WarmOpeningOverlayHoldMs = 72;
    private const double OpeningOverlayFadeDurationMs = 220.0;
    private const int OpeningOverlayFadeTickMs = 16;
    private const int OpeningRevealFallbackDelayMs = 48;
    private const int HiddenTrimDelayMs = 5000;
    private const int PreloadedSettingsTrimDelayMs = 60000;
    private const int SettingsPreloadDelayMs = 250;
    private const int BackdropApplyDelayMs = 180;
    private static readonly string[] SettingsPreloadTags = { "General", "Hotkey", "Wallpaper", "About", "Dashboard" };

    public static MainWindow Instance { get; private set; } = null!;

    internal static SettingsManager Settings => App.Settings;

    private volatile bool _isLight;
    private volatile bool _toggling;
    private TrayIconService _trayIcon = null!;
    private GlobalHotkeyService _globalHotkey = null!;
    private ScheduleSwitchService _scheduleSwitch = null!;
    private WallpaperSwitchService _wallpaperSwitch = null!;
    private ThemeSwitchService _themeSwitch = null!;

    private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);
    private WndProcDelegate? _wndProcDelegate;
    private nint _origWndProc;

    private static void TrimWorkingSet()
    {
        using var proc = Process.GetCurrentProcess();
        SetProcessWorkingSetSize(proc.Handle, (nint)(-1), (nint)(-1));
    }

    private static void CompactMemory(bool compactLargeObjects)
    {
        if (compactLargeObjects)
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: compactLargeObjects);
        GC.WaitForPendingFinalizers();
        TrimWorkingSet();
    }

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        Title = Localizer.T(Localizer.S_AppName);

        ExtendsContentIntoTitleBar = true;
        var presenter = AppWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
        if (presenter != null) presenter.IsMaximizable = true;

        AppWindow.IsShownInSwitchers = false;

        var hwnd = GetWindowHandle(this);
        SetWindowIcon(hwnd);

        var dpi = (float)GetDpiForWindow(hwnd) / 96f;
        AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(480 * dpi), (int)(560 * dpi)));

        _wndProcDelegate = WndProc;
        _origWndProc = SetWindowLongPtrW(hwnd, GWLP_WNDPROC, System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));

        _isLight = ThemeHelper.ReadCurrentThemeIsLight();
        ApplyOpeningBackground();
        _trayIcon = new TrayIconService(Settings, DispatcherQueue, () => _isLight, ToggleTheme, () => ShowSettings(), DoExit);
        _globalHotkey = new GlobalHotkeyService(Settings, DispatcherQueue, ToggleTheme);
        _scheduleSwitch = new ScheduleSwitchService(Settings, DispatcherQueue, () => _isLight, ToggleThemeFromSchedule);
        _wallpaperSwitch = new WallpaperSwitchService(Settings);
        _themeSwitch = new ThemeSwitchService(Settings);

        _trayIcon.Create();
        _globalHotkey.Register();
        StartScheduleTimer();

        AppWindow.Closing += (_, args) =>
        {
            args.Cancel = true;
            QueueHideToTray();
        };

        if (!Settings.FirstRunDone)
        {
            ShowOnboarding();
        }
        else
        {
            SetWindowCloaked(true);
            QueuePreloadSettingsPage();
        }
    }

    private void CancelPendingUiWork()
    {
        _trimTimer?.Dispose();
        _trimTimer = null;

        _revealFallbackTimer?.Stop();
        _revealFallbackTimer = null;

        _backdropTimer?.Stop();
        if (_backdropTimer != null)
            _backdropTimer.Tick -= OnBackdropTimerTick;
        _backdropTimer = null;

        _openingFadeTimer?.Stop();
        if (_openingFadeTimer != null)
            _openingFadeTimer.Tick -= OnOpeningFadeTimerTick;
        _openingFadeTimer = null;

        _openingFadeDelayTimer?.Stop();
        if (_openingFadeDelayTimer != null)
            _openingFadeDelayTimer.Tick -= OnOpeningFadeDelayTimerTick;
        _openingFadeDelayTimer = null;
        _openingOverlay.Dispose();

        if (_firstFrameHandler != null)
        {
            Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= _firstFrameHandler;
            _firstFrameHandler = null;
        }
    }

    private void ReleaseWindowResources()
    {
        SettingsPage.Instance?.ClearPageCache();
        UiCleanupHelper.ReleaseFrame(RootFrame);
        SystemBackdrop = null;
        SetTitleBar(null);
        _settingsContentReady = false;
        _windowRevealComplete = false;
        _settingsPreloadTagIndex = 0;
        _showSettingsAfterPreload = false;
        _pendingShowSettingsTag = null;
        SettingsPage.ClearInstance();
        DashboardPage.ClearInstance();
        WallpaperOverviewPage.ClearInstance();
    }

    private void ScheduleTrim(TimeSpan delay, bool compactLargeObjects)
    {
        _trimTimer?.Dispose();
        _trimTimer = new Timer(_ =>
        {
            if (_windowVisible || _preloadingSettings)
                return;

            CompactMemory(compactLargeObjects);
        }, null, delay, Timeout.InfiniteTimeSpan);
    }

    private void QueuePreloadSettingsPage()
    {
        _preloadSettingsTimer?.Stop();
        _preloadSettingsTimer = DispatcherQueue.CreateTimer();
        _preloadSettingsTimer.Interval = TimeSpan.FromMilliseconds(SettingsPreloadDelayMs);
        _preloadSettingsTimer.Tick += OnPreloadSettingsTimerTick;
        _preloadSettingsTimer.Start();
    }

    private void StopPreloadSettingsTimer()
    {
        _preloadSettingsTimer?.Stop();
        if (_preloadSettingsTimer != null)
            _preloadSettingsTimer.Tick -= OnPreloadSettingsTimerTick;
        _preloadSettingsTimer = null;
    }

    private void OnPreloadSettingsTimerTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        sender.Tick -= OnPreloadSettingsTimerTick;
        _preloadSettingsTimer = null;
        PreloadSettingsWindow();
    }

    private void PreloadSettingsWindow()
    {
        if (_windowVisible || RootFrame.Content is SettingsPage)
            return;

        _preloadingSettings = true;
        _settingsPreloadTagIndex = 0;
        _settingsContentReady = false;
        _windowRevealComplete = false;
        _isLight = ThemeHelper.ReadCurrentThemeIsLight();

        var hwnd = GetWindowHandle(this);
        int darkMode = _isLight ? 0 : 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
        ApplyWindowRequestedTheme();
        ApplyOpeningBackground();
        ApplyTitleBarColors();
        SetWindowCloaked(true);
        AppWindow.IsShownInSwitchers = false;
        LoadSettingsPageNow(null);
        AppWindow.Show();
    }

    private void HideToTray()
    {
        if (!_windowVisible)
            return;

        _windowVisible = false;
        ResetCaptionButtonState();
        SetWindowCloaked(true);
        AppWindow.IsShownInSwitchers = false;
        AppWindow.Hide();
        Settings.FlushPendingSave();
        CancelPendingUiWork();
        _wallpaperSwitch.ReleaseIfDisabled();
        _themeSwitch.ReleaseToast();
        ScheduleTrim(TimeSpan.FromMilliseconds(HiddenTrimDelayMs), compactLargeObjects: false);
    }

    private void RevealWindow(bool showImmediately)
    {
        var hwnd = GetWindowHandle(this);
        int darkMode = _isLight ? 0 : 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
        ApplyWindowRequestedTheme();
        ApplyOpeningBackground();
        ApplyTitleBarColors();
        ResetCaptionButtonState();
        AppWindow.IsShownInSwitchers = true;

        var (sw, sh) = GetStartupScreenSize();
        var (w, h) = GetScaledWindowSize(hwnd);
        var finalX = (sw - w) / 2;
        var finalY = (sh - h) / 2;
        SetWindowPos(hwnd, IntPtr.Zero, finalX, finalY, w, h, SWP_NOZORDER);

        if (showImmediately)
        {
            AppWindow.Show();
            DwmFlush();
            CompleteWindowReveal();
            ResetCaptionButtonState();
        }
        else
        {
            SetWindowCloaked(true);
            if (_firstFrameHandler != null)
                Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= _firstFrameHandler;
            _firstFrameHandler = (_, _) =>
            {
                Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= _firstFrameHandler;
                _firstFrameHandler = null;
                StopRevealFallback();
                DwmFlush();
                CompleteWindowReveal();
                ResetCaptionButtonState();
            };
            Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += _firstFrameHandler;
            AppWindow.Show();
            QueueRevealFallback();
        }

        SetWindowIcon(hwnd);
    }

    // ── Window proc ──

    private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case WM_ERASEBKGND:
                return 1;
            case WM_GETMINMAXINFO:
                var dpi = (float)GetDpiForWindow(hWnd) / 96f;
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                mmi.ptMinTrackSize = new POINT { X = (int)(480 * dpi), Y = (int)(560 * dpi) };
                Marshal.StructureToPtr(mmi, lParam, false);
                break;
            case WM_SETTINGCHANGE:
                SyncThemeFromSystem();
                break;
        }
        return CallWindowProcW(_origWndProc, hWnd, msg, wParam, lParam);
    }

    private void SyncThemeFromSystem()
    {
        if (_toggling) return;
        var newIsLight = ThemeHelper.ReadCurrentThemeIsLight();
        if (newIsLight == _isLight) return;
        _isLight = newIsLight;
        DispatcherQueue.TryEnqueue(() =>
        {
            _trayIcon.UpdateState();
            ApplyWindowRequestedTheme();
            ApplyTitleBarColors();
            var hwnd = GetWindowHandle(this);
            int darkMode = _isLight ? 0 : 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            SyncWallpaperForCurrentTheme();
            WallpaperOverviewPage.RefreshPreviewIfVisible();
            DashboardPage.RefreshPreviewIfVisible();
        });
    }

    // ── Global hotkey ──

    internal void RegisterGlobalHotkey() => _globalHotkey.Register();

    // ── Scheduled switch ──

    internal void StartScheduleTimer() => _scheduleSwitch.Start();

    internal void UpdateScheduleTimer() => _scheduleSwitch.Update();

    internal void SyncWallpaperForCurrentTheme()
    {
        if (Settings.AutoSwitchWallpaper)
            _wallpaperSwitch.ApplyForTheme(_isLight);
        else
            _wallpaperSwitch.ReleaseIfDisabled();
    }

    // ── Toggle ──

    internal void ToggleTheme()
    {
        DashboardPage.RefreshPreviewIfVisible();
        ApplyThemeMode(!_isLight);
    }

    private void ToggleThemeFromSchedule()
    {
        ApplyThemeMode(!_isLight);
    }

    internal bool CurrentThemeIsLight => _isLight;

    internal void ApplyThemeMode(bool isLight)
    {
        if (_toggling) return;

        _isLight = isLight;
        _toggling = true;

        // Start wallpaper transition first so it runs in parallel with UI update.
        _wallpaperSwitch.ApplyForTheme(isLight);

        _trayIcon.UpdateState();
        ApplyWindowRequestedTheme();
        ApplyTitleBarColors();

        var hwnd = GetWindowHandle(this);
        int darkMode = isLight ? 0 : 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

        _themeSwitch.ShowSwitchToast(isLight);

        WallpaperOverviewPage.RefreshPreviewIfVisible();
        DashboardPage.RefreshPreviewIfVisible();

        _themeSwitch.ApplySystemThemeAsync(isLight, () => _toggling = false);
    }

    private void ApplyTitleBarColors()
    {
        var tb = AppWindow.TitleBar;
        var transparent = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        tb.BackgroundColor = transparent;
        tb.InactiveBackgroundColor = transparent;
        if (_isLight)
        {
            tb.ForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            tb.InactiveForegroundColor = Windows.UI.Color.FromArgb(255, 150, 150, 150);
            tb.ButtonBackgroundColor = transparent;
            tb.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            tb.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(20, 0, 0, 0);
            tb.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            tb.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(30, 0, 0, 0);
            tb.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            tb.ButtonInactiveBackgroundColor = transparent;
            tb.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 150, 150, 150);
        }
        else
        {
            tb.ForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
            tb.InactiveForegroundColor = Windows.UI.Color.FromArgb(255, 100, 100, 100);
            tb.ButtonBackgroundColor = transparent;
            tb.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
            tb.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(20, 255, 255, 255);
            tb.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
            tb.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(30, 255, 255, 255);
            tb.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
            tb.ButtonInactiveBackgroundColor = transparent;
            tb.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 100, 100, 100);
        }
    }

    internal void RefreshWindowChrome()
    {
        ApplyTitleBarColors();
        ResetCaptionButtonState();
    }

    internal void QueueSettingsContentReady()
    {
        _settingsContentReady = true;

        if (_preloadingSettings && !_windowVisible)
        {
            ContinueSettingsPagePreload();
            return;
        }

        QueueApplyStableBackdrop();

        if (_openingOverlay.IsVisible && _windowRevealComplete)
            QueueFadeNativeOpeningOverlay(TimeSpan.FromMilliseconds(OpeningOverlayFadeDelayMs));

    }

    private void SetWindowCloaked(bool cloaked)
    {
        int cloak = cloaked ? 1 : 0;
        DwmSetWindowAttribute(GetWindowHandle(this), DWMWA_CLOAK, ref cloak, sizeof(int));
    }

    private void FinishSettingsPreload()
    {
        var shouldShowAfterPreload = _showSettingsAfterPreload;
        var pendingShowTag = _pendingShowSettingsTag;
        _showSettingsAfterPreload = false;
        _pendingShowSettingsTag = null;

        _preloadingSettings = false;
        _settingsPreloadTagIndex = 0;
        SettingsPage.Instance?.NavigateTo(string.IsNullOrWhiteSpace(pendingShowTag) ? "Dashboard" : pendingShowTag);

        if (shouldShowAfterPreload)
        {
            ShowSettings(pendingShowTag);
            return;
        }

        SystemBackdrop = null;
        SetWindowCloaked(true);
        AppWindow.IsShownInSwitchers = false;
        AppWindow.Hide();
        ScheduleTrim(TimeSpan.FromMilliseconds(PreloadedSettingsTrimDelayMs), compactLargeObjects: false);
    }

    private void ContinueSettingsPagePreload()
    {
        if (!_preloadingSettings || _windowVisible)
            return;

        if (_settingsPreloadTagIndex >= SettingsPreloadTags.Length)
        {
            FinishSettingsPreload();
            return;
        }

        var tag = SettingsPreloadTags[_settingsPreloadTagIndex++];
        _settingsContentReady = false;
        DispatcherQueue.TryEnqueue(() =>
        {
            if (!_preloadingSettings || _windowVisible)
                return;

            SettingsPage.Instance?.NavigateTo(tag);
        });
    }

    private void CompleteWindowReveal()
    {
        _windowRevealComplete = true;
        SetWindowCloaked(false);
        if (_settingsContentReady)
            QueueFadeNativeOpeningOverlay(TimeSpan.FromMilliseconds(OpeningOverlayFadeDelayMs));
    }

    private void ApplyOpeningBackground()
    {
        var color = _isLight
            ? Microsoft.UI.Colors.White
            : Windows.UI.Color.FromArgb(255, 32, 32, 32);

        var brush = new SolidColorBrush(color);
        WindowRoot.Background = brush;
        RootFrame.Background = brush;
    }

    private void ApplyWindowRequestedTheme()
    {
        var theme = _isLight ? ElementTheme.Light : ElementTheme.Dark;
        WindowRoot.RequestedTheme = theme;
        if (RootFrame.Content is FrameworkElement content)
            content.RequestedTheme = theme;
    }

    private void QueueApplyStableBackdrop()
    {
        _backdropTimer?.Stop();
        _backdropTimer = DispatcherQueue.CreateTimer();
        _backdropTimer.Interval = TimeSpan.FromMilliseconds(BackdropApplyDelayMs);
        _backdropTimer.Tick += OnBackdropTimerTick;
        _backdropTimer.Start();
    }

    private void OnBackdropTimerTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        sender.Tick -= OnBackdropTimerTick;
        _backdropTimer = null;

        if (_windowVisible && !_preloadingSettings && SystemBackdrop == null)
            SystemBackdrop = new MicaBackdrop();
    }

    private void LoadSettingsPageNow(string? initialTag)
    {
        RootFrame.CacheSize = 1;
        if (RootFrame.Content is not SettingsPage)
        {
            SettingsPage.PendingLaunchTag = initialTag;
            RootFrame.Navigate(typeof(SettingsPage));
        }
        else if (!string.IsNullOrWhiteSpace(initialTag))
        {
            SettingsPage.Instance?.NavigateTo(initialTag);
        }
    }

    private void ResetCaptionButtonState()
    {
        var hwnd = GetWindowHandle(this);
        ReleaseCapture();
        SendMessageW(hwnd, WM_CANCELMODE, 0, 0);
        SendMessageW(hwnd, WM_NCMOUSELEAVE, 0, 0);
        SendMessageW(hwnd, WM_MOUSELEAVE, 0, 0);
        ApplyTitleBarColors();
    }

    private void QueueHideToTray()
    {
        ResetCaptionButtonState();
        if (_hideToTrayQueued)
            return;

        _hideToTrayQueued = true;
        _hideToTrayTimer?.Stop();
        _hideToTrayTimer = DispatcherQueue.CreateTimer();
        _hideToTrayTimer.Interval = TimeSpan.FromMilliseconds(80);
        _hideToTrayTimer.Tick += OnHideToTrayTimerTick;
        _hideToTrayTimer.Start();
    }

    private void QueueRevealFallback()
    {
        _revealFallbackTimer?.Stop();
        _revealFallbackTimer = DispatcherQueue.CreateTimer();
        _revealFallbackTimer.Interval = TimeSpan.FromMilliseconds(OpeningRevealFallbackDelayMs);
        _revealFallbackTimer.Tick += OnRevealFallbackTimerTick;
        _revealFallbackTimer.Start();
    }

    private void StopRevealFallback()
    {
        _revealFallbackTimer?.Stop();
        if (_revealFallbackTimer != null)
            _revealFallbackTimer.Tick -= OnRevealFallbackTimerTick;
        _revealFallbackTimer = null;
    }

    private void OnRevealFallbackTimerTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        sender.Tick -= OnRevealFallbackTimerTick;
        _revealFallbackTimer = null;

        if (!_windowVisible)
            return;

        if (_firstFrameHandler != null)
        {
            Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= _firstFrameHandler;
            _firstFrameHandler = null;
        }

        DwmFlush();
        CompleteWindowReveal();
        ResetCaptionButtonState();
    }

    private void QueueFadeNativeOpeningOverlay(TimeSpan delay)
    {
        if (!_openingOverlay.IsVisible)
            return;

        if (_openingFadeTimer != null)
            return;

        var effectiveDelay = delay;
        var remainingMinimumHold = GetRemainingOpeningOverlayMinimumHold();
        if (remainingMinimumHold > effectiveDelay)
            effectiveDelay = remainingMinimumHold;

        _openingFadeDelayTimer?.Stop();
        if (_openingFadeDelayTimer != null)
            _openingFadeDelayTimer.Tick -= OnOpeningFadeDelayTimerTick;
        _openingFadeDelayTimer = DispatcherQueue.CreateTimer();
        _openingFadeDelayTimer.Interval = effectiveDelay;
        _openingFadeDelayTimer.Tick += OnOpeningFadeDelayTimerTick;
        _openingFadeDelayTimer.Start();
    }

    private void OnOpeningFadeDelayTimerTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        sender.Tick -= OnOpeningFadeDelayTimerTick;
        _openingFadeDelayTimer = null;
        StartOpeningFade();
    }

    private void StartOpeningFade()
    {
        if (!_openingOverlay.IsVisible)
            return;

        _openingFadeStarted = Stopwatch.GetTimestamp();
        _openingOverlayMinimumHold = TimeSpan.Zero;
        _openingFadeTimer?.Stop();
        if (_openingFadeTimer != null)
            _openingFadeTimer.Tick -= OnOpeningFadeTimerTick;

        _openingFadeTimer = DispatcherQueue.CreateTimer();
        _openingFadeTimer.Interval = TimeSpan.FromMilliseconds(OpeningOverlayFadeTickMs);
        _openingFadeTimer.Tick += OnOpeningFadeTimerTick;
        _openingFadeTimer.Start();
        _openingOverlay.SetOpacity(255);
    }

    private void OnOpeningFadeTimerTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        var elapsed = Stopwatch.GetElapsedTime(_openingFadeStarted);
        var progress = Math.Clamp(elapsed.TotalMilliseconds / OpeningOverlayFadeDurationMs, 0, 1);
        var easedProgress = 1 - Math.Pow(1 - progress, 3);
        var opacity = (byte)Math.Clamp((int)Math.Round(255 * (1 - easedProgress)), 0, 255);
        _openingOverlay.SetOpacity(opacity);

        if (progress < 1)
            return;

        sender.Stop();
        sender.Tick -= OnOpeningFadeTimerTick;
        _openingFadeTimer = null;
        _openingOverlay.Dispose();
    }

    private TimeSpan GetRemainingOpeningOverlayMinimumHold()
    {
        if (_openingOverlayShownAt == 0 || _openingOverlayMinimumHold <= TimeSpan.Zero)
            return TimeSpan.Zero;

        var elapsed = Stopwatch.GetElapsedTime(_openingOverlayShownAt);
        if (elapsed >= _openingOverlayMinimumHold)
            return TimeSpan.Zero;

        return _openingOverlayMinimumHold - elapsed;
    }

    private void OnHideToTrayTimerTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        sender.Tick -= OnHideToTrayTimerTick;
        _hideToTrayTimer = null;
        _hideToTrayQueued = false;
        HideToTray();
    }

    // ── Exit ──

    private void DoExit()
    {
        _globalHotkey.Dispose();
        _scheduleSwitch.Dispose();
        Settings.FlushPendingSave();
        CancelPendingUiWork();
        StopPreloadSettingsTimer();
        _trayIcon.Dispose();
        ReleaseWindowResources();
        _openingOverlay.Dispose();
        _wallpaperSwitch.Release();
        ReleaseWindowIcon();
        Environment.Exit(0);
    }

    // ── Show settings window ──

    private EventHandler<object>? _firstFrameHandler;
    private readonly OpeningOverlayWindow _openingOverlay = new();
    private bool _windowVisible;
    private bool _hideToTrayQueued;
    private bool _settingsContentReady;
    private bool _windowRevealComplete;
    private bool _preloadingSettings;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _hideToTrayTimer;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _preloadSettingsTimer;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _revealFallbackTimer;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _backdropTimer;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _openingFadeDelayTimer;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _openingFadeTimer;
    private Timer? _trimTimer;
    private long _openingFadeStarted;
    private long _openingOverlayShownAt;
    private TimeSpan _openingOverlayMinimumHold;
    private int _settingsPreloadTagIndex;
    private bool _showSettingsAfterPreload;
    private string? _pendingShowSettingsTag;

    public void ShowSettings(string? initialTag = null)
    {
        if (_windowVisible)
        {
            BringWindowToTop(GetWindowHandle(this));
            SetForegroundWindow(GetWindowHandle(this));
            if (!string.IsNullOrWhiteSpace(initialTag))
                SettingsPage.Instance?.NavigateTo(initialTag);
            return;
        }

        if (_preloadingSettings && RootFrame.Content is SettingsPage)
        {
            _showSettingsAfterPreload = true;
            _pendingShowSettingsTag = initialTag;
            return;
        }

        StopPreloadSettingsTimer();
        CancelPendingUiWork();
        _preloadingSettings = false;
        _settingsPreloadTagIndex = 0;
        _windowVisible = true;
        _windowRevealComplete = false;
        _isLight = ThemeHelper.ReadCurrentThemeIsLight();
        ApplyOpeningBackground();
        ApplyWindowRequestedTheme();
        var hwnd = GetWindowHandle(this);
        var hasWarmSettingsPage = RootFrame.Content is SettingsPage;
        _settingsContentReady = hasWarmSettingsPage;
        var (overlayWidth, overlayHeight) = GetScaledWindowSize(hwnd);
        var (screenWidth, screenHeight) = GetStartupScreenSize();
        var finalX = (screenWidth - overlayWidth) / 2;
        var finalY = (screenHeight - overlayHeight) / 2;
        _openingOverlay.Show(_isLight, finalX, finalY, overlayWidth, overlayHeight);
        _openingOverlayShownAt = Stopwatch.GetTimestamp();
        _openingOverlayMinimumHold = TimeSpan.FromMilliseconds(hasWarmSettingsPage ? WarmOpeningOverlayHoldMs : 0);

        _trayIcon.UpdateState();
        LoadSettingsPageNow(initialTag);
        SettingsPage.Instance?.NavigateTo(string.IsNullOrWhiteSpace(initialTag) ? "Dashboard" : initialTag);
        RevealWindow(showImmediately: hasWarmSettingsPage);
        QueueFadeNativeOpeningOverlay(TimeSpan.FromMilliseconds(
            hasWarmSettingsPage ? WarmOpeningOverlayHoldMs : OpeningOverlayMaxHoldMs));
    }

    public void ShowOnboarding()
    {
        _windowVisible = true;
        _isLight = ThemeHelper.ReadCurrentThemeIsLight();
        _trayIcon.UpdateState();
        RootFrame.Navigate(typeof(OnboardingPage));
        RevealWindow(showImmediately: true);
    }

    public async void ShowTrayTutorial()
    {
        try
        {
            // Wait for XamlRoot to be ready (SettingsPage may still be loading)
            for (int i = 0; i < 10; i++)
            {
                if (RootFrame.XamlRoot != null) break;
                await Task.Delay(200);
            }
            if (RootFrame.XamlRoot == null) return;

        var steps = new StackPanel { Spacing = 12, MaxWidth = 360 };

        steps.Children.Add(new TextBlock
        {
            Text = Localizer.T(Localizer.S_OnTrayDesc),
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.8,
        });

        AddStep(steps, "1", Localizer.T(Localizer.S_TrayStep1));
        AddStep(steps, "2", Localizer.T(Localizer.S_TrayStep2));
        AddStep(steps, "3", Localizer.T(Localizer.S_TrayStep3));

        // Screenshot guide
        var imgPath = Path.Combine(AppContext.BaseDirectory, "Assets", "tray-guide.png");
        if (File.Exists(imgPath))
        {
            steps.Children.Add(new Image
            {
                Source = new BitmapImage(new Uri(imgPath)),
                MaxWidth = 400,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0),
            });
        }

        var dialog = new ContentDialog
        {
            Title = Localizer.T(Localizer.S_OnTrayTitle),
            Content = steps,
            CloseButtonText = Localizer.T(Localizer.S_OK),
            XamlRoot = RootFrame.XamlRoot,
        };
        await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Tray tutorial failed");
        }
    }

    private static void AddStep(StackPanel parent, string num, string text)
    {
        var grid = new Grid { ColumnSpacing = 10 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var badge = new Border
        {
            Background = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"],
            CornerRadius = new CornerRadius(12),
            Width = 24, Height = 24,
            Child = new TextBlock
            {
                Text = num,
                FontSize = 12,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        Grid.SetColumn(badge, 0);
        grid.Children.Add(badge);

        var textBlock = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(textBlock, 1);
        grid.Children.Add(textBlock);

        parent.Children.Add(grid);
    }
}
