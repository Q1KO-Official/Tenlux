using Microsoft.Win32;
using static Tenlux.Helpers.NativeMethods;

namespace Tenlux.Helpers;

internal static class StartupHelper
{
    private static string StartupValueName =>
        Environment.GetEnvironmentVariable("TENLUX_STARTUP_VALUE_NAME") ?? NativeMethods.StartupValueName;

    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegPath);
            return key?.GetValue(StartupValueName) != null;
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Startup state read failed");
            return false;
        }
    }

    public static void SetStartupEnabled(bool enabled, string exePath)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(StartupRegPath);
            if (key == null) return;
            if (enabled)
                key.SetValue(StartupValueName, $"\"{exePath}\"", RegistryValueKind.String);
            else
                key.DeleteValue(StartupValueName, false);
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Startup state write failed");
        }
    }
}
