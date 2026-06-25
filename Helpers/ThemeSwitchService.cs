namespace Tenlux.Helpers;

internal sealed class ThemeSwitchService
{
    private readonly SettingsManager _settings;

    public ThemeSwitchService(SettingsManager settings)
    {
        _settings = settings;
    }

    public void ShowSwitchToast(bool isLight)
    {
        if (!_settings.ToastNotification) return;

        var message = isLight
            ? Localizer.T(Localizer.S_ToastLightSwitched)
            : Localizer.T(Localizer.S_ToastDarkSwitched);
        ToastHelper.ShowToast(message, _settings.ToastSound);
    }

    public void ApplySystemThemeAsync(bool isLight, Action onCompleted)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                ThemeHelper.ApplyThemeToggle(isLight ? 1 : 0);
            }
            finally
            {
                onCompleted();
            }
        });
    }

    public void ReleaseToast()
    {
        ToastHelper.Release();
    }
}
