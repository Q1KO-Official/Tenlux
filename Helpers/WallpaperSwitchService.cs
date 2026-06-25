namespace Tenlux.Helpers;

internal sealed class WallpaperSwitchService
{
    private readonly SettingsManager _settings;
    private volatile int _version;

    public WallpaperSwitchService(SettingsManager settings)
    {
        _settings = settings;
    }

    public void ApplyForTheme(bool isLight)
    {
        if (!_settings.AutoSwitchWallpaper) return;

        var version = Interlocked.Increment(ref _version);
        ThreadPool.QueueUserWorkItem(_ => ApplyForThemeCore(isLight, version));
    }

    public void ReleaseIfDisabled()
    {
        if (!_settings.AutoSwitchWallpaper)
            WallpaperHelper.Release();
    }

    public void Release()
    {
        WallpaperHelper.Release();
    }

    private void ApplyForThemeCore(bool isLight, int version)
    {
        if (version != _version) return;

        var theme = _settings.Themes.FirstOrDefault(t => t.IsEnabled && HasWallpaper(t))
            ?? _settings.Themes.FirstOrDefault(t => t.IsEnabled);
        if (theme == null) return;

        _settings.CaptureOriginalWallpaperIfNeeded();
        WallpaperHelper.SetWallpaper(isLight, theme.DarkWallpaper, theme.LightWallpaper);
        WallpaperHelper.SetWallpaperPosition(theme.WallpaperStyle);
    }

    private static bool HasWallpaper(WallpaperTheme theme) =>
        !string.IsNullOrEmpty(theme.DarkWallpaper) || !string.IsNullOrEmpty(theme.LightWallpaper);
}
