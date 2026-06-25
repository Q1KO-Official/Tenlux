using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Tenlux.Helpers;
using System.Threading;
using System.Diagnostics;
using static Tenlux.Helpers.NativeMethods;

namespace Tenlux;

public partial class App : Application
{
    private static Mutex? _appMutex;
    private static EventWaitHandle? _showSettingsSignal;
    private static Thread? _showSettingsListener;
    private static string InstanceSuffix => Environment.GetEnvironmentVariable("TENLUX_INSTANCE_SUFFIX") ?? string.Empty;
    private static string InstanceMutexName => string.IsNullOrWhiteSpace(InstanceSuffix) ? "Tenlux_Unique" : $"Tenlux_Unique_{InstanceSuffix}";
    private static string ShowSettingsSignalName => string.IsNullOrWhiteSpace(InstanceSuffix) ? "Tenlux_ShowSettings" : $"Tenlux_ShowSettings_{InstanceSuffix}";

    internal static SettingsManager Settings { get; } = new();
    internal static DispatcherQueue? MainDispatcher { get; private set; }
    private static string? _pendingStartupTag;

    public App()
    {
        RequestedTheme = ThemeHelper.ReadCurrentThemeIsLight()
            ? ApplicationTheme.Light
            : ApplicationTheme.Dark;
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _pendingStartupTag = ParseLaunchTarget(args.Arguments) ?? ParseLaunchTarget(Environment.GetCommandLineArgs().Skip(1));
        NativeMethods.RefreshStartupScreenMetrics();

        bool createdNew;
        _appMutex = new Mutex(true, InstanceMutexName, out createdNew);
        if (!createdNew)
        {
            var notifiedExistingInstance = false;
            try
            {
                using var signal = EventWaitHandle.OpenExisting(ShowSettingsSignalName);
                signal.Set();
                notifiedExistingInstance = true;
            }
            catch
            {
                // If the existing instance is from an older build without the signal,
                // exiting is still the least disruptive fallback.
            }

            if (!notifiedExistingInstance)
            {
                var otherInstancePath = TryGetOtherInstancePath();
                var message = otherInstancePath == null
                    ? "Tenlux is already running in the system tray."
                    : $"Another Tenlux instance is already running.\n\nPath:\n{otherInstancePath}\n\nClose that instance if you want to launch this build.";

                MessageBoxW(IntPtr.Zero,
                    message,
                    ProductInfo.Name,
                    MB_OK | MB_ICONINFORMATION);
            }

            _appMutex.Dispose();
            Environment.Exit(0);
        }

        Settings.Load();
        var isFirstRun = !Settings.FirstRunDone;
        if (isFirstRun)
            StartupHelper.SetStartupEnabled(false, Environment.ProcessPath ?? "");
        var window = new MainWindow();
        MainDispatcher = window.DispatcherQueue;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOREDIRECTIONBITMAP);

        int darkMode = ThemeHelper.ReadCurrentThemeIsLight() ? 0 : 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

        StartShowSettingsSignalListener();

        if (isFirstRun)
        {
            window.Activate();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_pendingStartupTag))
                window.AppWindow.Hide();
            else
                window.ShowSettings(_pendingStartupTag);
        }
    }

    private static void StartShowSettingsSignalListener()
    {
        _showSettingsSignal ??= new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsSignalName);
        if (_showSettingsListener != null) return;

        _showSettingsListener = new Thread(() =>
        {
            while (true)
            {
                _showSettingsSignal.WaitOne();
                MainDispatcher?.TryEnqueue(() => MainWindow.Instance.ShowSettings());
            }
        })
        {
            IsBackground = true,
            Name = "Tenlux.ShowSettingsSignal",
        };
        _showSettingsListener.Start();
    }

    private static string? TryGetOtherInstancePath()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName(ProductInfo.Name))
            {
                if (process.Id == Environment.ProcessId)
                    continue;

                try
                {
                    var path = process.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(path))
                        return path;
                }
                catch
                {
                    // Best effort only.
                }
            }
        }
        catch
        {
            // Best effort only.
        }

        return null;
    }

    private static string? ParseLaunchTarget(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments)) return null;

        var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return ParseLaunchTarget(parts);
    }

    private static string? ParseLaunchTarget(IEnumerable<string> parts)
    {
        foreach (var part in parts)
        {
            if (part.StartsWith("--open=", StringComparison.OrdinalIgnoreCase))
                return part["--open=".Length..];
        }

        return null;
    }
}
