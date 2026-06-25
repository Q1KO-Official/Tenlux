namespace Tenlux.Helpers;

internal static class AppLogger
{
    private static readonly object LogLock = new();
    private const long MaxLogBytes = 262144;
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        ProductInfo.Name,
        $"{ProductInfo.Name}.log");

    public static string CurrentLogPath => LogPath;

    public static void Log(Exception ex, string context)
    {
        Log($"{context}: {ex}");
    }

    public static void Log(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            lock (LogLock)
            {
                RotateIfNeeded();
                File.AppendAllText(LogPath, $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never break app behavior.
        }
    }

    private static void RotateIfNeeded()
    {
        if (!File.Exists(LogPath)) return;

        var info = new FileInfo(LogPath);
        if (info.Length <= MaxLogBytes) return;

        var archivePath = LogPath + ".1";
        if (File.Exists(archivePath))
            File.Delete(archivePath);

        File.Move(LogPath, archivePath);
    }
}
