namespace Tenlux.Helpers;

internal class WallpaperTheme
{
    public string Name { get; set; } = "";
    public string DarkWallpaper { get; set; } = "";
    public string LightWallpaper { get; set; } = "";
    public int WallpaperStyle { get; set; } // 0=Fill 1=Fit 2=Stretch 3=Tile
    public bool IsEnabled { get; set; }

    public WallpaperTheme Clone() => new()
    {
        Name = Name,
        DarkWallpaper = DarkWallpaper,
        LightWallpaper = LightWallpaper,
        WallpaperStyle = WallpaperStyle,
        IsEnabled = IsEnabled,
    };
}
