using Microsoft.Win32;
using static Tenlux.Helpers.NativeMethods;

namespace Tenlux.Helpers;

internal static class ThemeHelper
{
    public static bool ReadCurrentThemeIsLight()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(ThemeRegPath);
            if (key?.GetValue("SystemUsesLightTheme") is int v) return v == 1;
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Theme read failed");
        }
        return true;
    }

    public static void ApplyThemeToggle(int newValue)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(ThemeRegPath, true);
            if (key != null)
            {
                key.SetValue("SystemUsesLightTheme", newValue, RegistryValueKind.DWord);
                key.SetValue("AppsUseLightTheme", newValue, RegistryValueKind.DWord);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Theme write failed");
        }
        SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero,
            "ImmersiveColorSet", SMTO_ABORTIFHUNG, 1, out _);
        SendMessageTimeout(HWND_BROADCAST, WM_THEMECHANGED, IntPtr.Zero,
            null, SMTO_ABORTIFHUNG, 1, out _);
    }
}
