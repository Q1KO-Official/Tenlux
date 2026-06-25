using System.Runtime.InteropServices;
using System.Text;
using static Tenlux.Helpers.NativeMethods;

namespace Tenlux.Helpers;

internal static class WallpaperHelper
{
    private static IDesktopWallpaper? _desktopWallpaper;
    private static readonly object _wallpaperLock = new();

    public static void Release()
    {
        lock (_wallpaperLock)
        {
            if (_desktopWallpaper != null)
            {
                Marshal.ReleaseComObject(_desktopWallpaper);
                _desktopWallpaper = null;
            }
        }
    }
    private static readonly DWPosition[] _wallpaperPositions = {
        DWPosition.Fill, DWPosition.Fit, DWPosition.Stretch, DWPosition.Tile
    };

    private static IDesktopWallpaper GetDesktopWallpaper()
    {
        if (_desktopWallpaper == null)
        {
            lock (_wallpaperLock)
            {
                _desktopWallpaper ??= (IDesktopWallpaper)new DesktopWallpaperClass();
            }
        }
        return _desktopWallpaper;
    }

    public static void SetWallpaperPosition(int styleIndex)
    {
        if (styleIndex < 0 || styleIndex >= _wallpaperPositions.Length) return;
        SetWallpaperPosition(_wallpaperPositions[styleIndex]);
    }

    public static void SetWallpaperPosition(DWPosition position)
    {
        try
        {
            GetDesktopWallpaper().SetPosition(position);
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Wallpaper position set failed");
            lock (_wallpaperLock)
            {
                if (_desktopWallpaper != null) { try { Marshal.ReleaseComObject(_desktopWallpaper); } catch { } _desktopWallpaper = null; }
            }
            try { GetDesktopWallpaper().SetPosition(position); }
            catch (Exception retryEx)
            {
                AppLogger.Log(retryEx, "Wallpaper position retry failed");
                lock (_wallpaperLock)
                {
                    if (_desktopWallpaper != null) { try { Marshal.ReleaseComObject(_desktopWallpaper); } catch { } _desktopWallpaper = null; }
                }
            }
        }
    }

    public static bool SetWallpaper(bool isLight, string darkWallpaper, string lightWallpaper)
    {
        string path = isLight ? lightWallpaper : darkWallpaper;
        return SetWallpaperPath(path);
    }

    public static bool SetWallpaperPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
        try
        {
            EnableWorkerW();
            var iad = (IActiveDesktop)new ActiveDesktopClass();
            try
            {
                iad.SetWallpaper(path, 0);
                iad.ApplyChanges(AD_APPLY_ALL);
            }
            finally
            {
                Marshal.ReleaseComObject(iad);
            }
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "ActiveDesktop wallpaper set failed");
            return SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }

    public static bool TryGetCurrentWallpaper(out string path)
    {
        path = "";
        try
        {
            path = GetDesktopWallpaper().GetWallpaper(null!);
            if (!string.IsNullOrWhiteSpace(path))
                return true;
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "DesktopWallpaper wallpaper read failed");
            ResetDesktopWallpaperCom();
        }

        try
        {
            var iad = (IActiveDesktop)new ActiveDesktopClass();
            try
            {
                var buffer = new StringBuilder(4096);
                if (iad.GetWallpaper(buffer, buffer.Capacity, 0) == 0)
                {
                    path = buffer.ToString();
                    if (!string.IsNullOrWhiteSpace(path))
                        return true;
                }
            }
            finally
            {
                Marshal.ReleaseComObject(iad);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "ActiveDesktop wallpaper read failed");
        }

        try
        {
            path = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(@"Control Panel\Desktop")
                ?.GetValue("WallPaper") as string ?? "";
            return !string.IsNullOrWhiteSpace(path);
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Registry wallpaper read failed");
            return false;
        }
    }

    public static bool TryGetCurrentWallpaperPosition(out DWPosition position)
    {
        position = DWPosition.Fill;
        try
        {
            position = GetDesktopWallpaper().GetPosition();
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Wallpaper position read failed");
            ResetDesktopWallpaperCom();
            return false;
        }
    }

    private static void ResetDesktopWallpaperCom()
    {
        lock (_wallpaperLock)
        {
            if (_desktopWallpaper == null) return;
            try { Marshal.ReleaseComObject(_desktopWallpaper); }
            catch { }
            _desktopWallpaper = null;
        }
    }
}
