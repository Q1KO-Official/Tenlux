using Microsoft.UI.Dispatching;
using Microsoft.Win32;

namespace Tenlux.Helpers;

internal sealed class ScheduleSwitchService : IDisposable
{
    private readonly SettingsManager _settings;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Func<bool> _isLight;
    private readonly Action _toggleTheme;
    private readonly object _timerLock = new();

    private Timer? _timer;
    private bool _powerModeSubscribed;

    public ScheduleSwitchService(
        SettingsManager settings,
        DispatcherQueue dispatcherQueue,
        Func<bool> isLight,
        Action toggleTheme)
    {
        _settings = settings;
        _dispatcherQueue = dispatcherQueue;
        _isLight = isLight;
        _toggleTheme = toggleTheme;
    }

    public void Start()
    {
        if (!_powerModeSubscribed)
        {
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            _powerModeSubscribed = true;
        }
        Update();
    }

    public void Update()
    {
        ClearTimer();
        if (_settings.ScheduledSwitch)
            ScheduleNext();
    }

    public void Dispose()
    {
        if (_powerModeSubscribed)
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            _powerModeSubscribed = false;
        }
        ClearTimer();
    }

    private void ClearTimer()
    {
        lock (_timerLock)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }

    private void ScheduleNext(bool skipCorrection = false)
    {
        ClearTimer();
        if (!_settings.ScheduledSwitch) return;
        if (!TimeOnly.TryParse(_settings.LightTime, out var lightTime) ||
            !TimeOnly.TryParse(_settings.DarkTime, out var darkTime))
            return;

        var now = DateTime.Now;
        var today = now.Date;
        var lightMoment = today + lightTime.ToTimeSpan();
        var darkMoment = today + darkTime.ToTimeSpan();

        if (!skipCorrection)
        {
            var shouldBeLight = lightMoment > darkMoment
                ? now >= lightMoment || now < darkMoment
                : now >= lightMoment && now < darkMoment;

            if (shouldBeLight != _isLight())
                _dispatcherQueue.TryEnqueue(() => _toggleTheme());
        }

        var nextLight = lightMoment > now ? lightMoment : lightMoment.AddDays(1);
        var nextDark = darkMoment > now ? darkMoment : darkMoment.AddDays(1);
        var target = nextLight < nextDark ? nextLight : nextDark;
        var switchToLight = target == nextLight;
        var delay = target - now;

        lock (_timerLock)
        {
            _timer = new Timer(_ =>
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (switchToLight != _isLight())
                        _toggleTheme();
                    ScheduleNext(skipCorrection: true);
                });
            }, null, delay, Timeout.InfiniteTimeSpan);
        }
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
            _dispatcherQueue.TryEnqueue(() => ScheduleNext());
    }
}
