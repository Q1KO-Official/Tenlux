namespace Tenlux.Helpers;

internal class SettingsManager
{
    private System.Threading.Timer? _saveTimer;
    private readonly object _saveLock = new();
    private readonly object _fileLock = new();
    private const string PreviewPresetName = "测试版内置预设";
    private const string PreviewDarkWallpaperFileName = "PreviewGreenDark.jpg";
    private const string PreviewLightWallpaperFileName = "PreviewGreenLight.jpg";
    private const int PreviewWallpaperStyle = 0;

    public void SaveDebounced()
    {
        lock (_saveLock)
        {
            _saveTimer?.Dispose();
            _saveTimer = new System.Threading.Timer(_ =>
            {
                lock (_saveLock)
                {
                    _saveTimer?.Dispose();
                    _saveTimer = null;
                }

                if (App.MainDispatcher?.TryEnqueue(() => Save()) != true)
                    Save();
            }, null, 500, System.Threading.Timeout.Infinite);
        }
    }

    public void FlushPendingSave()
    {
        lock (_saveLock)
        {
            _saveTimer?.Dispose();
            _saveTimer = null;
        }

        Save();
    }

    public void ResetToDefaults()
    {
        SingleClickToggle = false;
        TrayClickEnabled = true;
        AutoSwitchWallpaper = false;
        ScheduledSwitch = false;
        LightTime = "07:00";
        DarkTime = "19:00";
        GlobalHotkey = false;
        HotkeyText = "Ctrl+Alt+D";
        DisableHotkeyInFullscreen = false;
        ToastNotification = false;
        ToastSound = false;

        Themes = new WallpaperTheme[4]
        {
            new() { Name = "1" },
            new() { Name = "" },
            new() { Name = "" },
            new() { Name = "" },
        };
        SeedPreviewWallpaperPresetIfNeeded();
        NormalizeWallpaperPresetState();
        Save();
    }

    public bool SingleClickToggle { get; set; }
    public bool TrayClickEnabled { get; set; } = true;
    public bool AutoSwitchWallpaper { get; set; }
    public bool ScheduledSwitch { get; set; }
    public string LightTime { get; set; } = "07:00";
    public string DarkTime { get; set; } = "19:00";
    public bool GlobalHotkey { get; set; }
    public string HotkeyText { get; set; } = "Ctrl+Alt+D";
    public bool DisableHotkeyInFullscreen { get; set; }
    public bool ToastNotification { get; set; }
    public bool ToastSound { get; set; }
    public bool FirstRunDone { get; set; }
    public bool OriginalWallpaperCaptured { get; set; }
    public string OriginalWallpaper { get; set; } = "";
    public int OriginalWallpaperPosition { get; set; } = -1;

    // Legacy fields (kept for migration, not used in new code)
    public string DarkWallpaper { get; set; } = "";
    public string LightWallpaper { get; set; } = "";
    public int WallpaperStyle { get; set; } = 1;

    // 4 wallpaper themes
    public WallpaperTheme[] Themes { get; set; } = new WallpaperTheme[4]
    {
        new() { Name = "1" },
        new() { Name = "" },
        new() { Name = "" },
        new() { Name = "" },
    };

    private static readonly string ConfigDir = ResolveConfigDir();
    internal static readonly string WallpaperDir = Path.Combine(ConfigDir, "Wallpapers");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, $"{ProductInfo.Name}.cfg");
    private static readonly string LegacyConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "ToggleDarkMode", "ToggleDarkMode.cfg");

    private static string ResolveConfigDir()
    {
        var overrideDir = Environment.GetEnvironmentVariable("TENLUX_CONFIG_DIR");
        return string.IsNullOrWhiteSpace(overrideDir)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ProductInfo.Name)
            : overrideDir;
    }

    internal static string CurrentConfigPath => ConfigPath;

    public void Load()
    {
        try
        {
            var shouldSaveMigratedConfig = false;

            // Migrate from legacy Documents path to AppData
            if (!File.Exists(ConfigPath) && File.Exists(LegacyConfigPath))
            {
                Directory.CreateDirectory(ConfigDir);
                File.Copy(LegacyConfigPath, ConfigPath, false);
                shouldSaveMigratedConfig = true;
            }
            if (!File.Exists(ConfigPath))
            {
                var changed = SeedPreviewWallpaperPresetIfNeeded();
                if (NormalizeWallpaperPresetState())
                    changed = true;
                if (changed)
                    Save();
                return;
            }
            foreach (var line in File.ReadAllLines(ConfigPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
                var eq = trimmed.IndexOf('=');
                if (eq < 0) continue;
                var key = trimmed[..eq].Trim();
                var val = trimmed[(eq + 1)..].Trim();
                if (key is "ScenePresets" or "CurrentScenePresetId")
                {
                    shouldSaveMigratedConfig = true;
                    continue;
                }

                if (int.TryParse(val, out var iv))
                {
                    switch (key)
                    {
                        case "SingleClickToggle": SingleClickToggle = iv != 0; break;
                        case "TrayClickEnabled": TrayClickEnabled = iv != 0; break;
                        case "AutoSwitchWallpaper": AutoSwitchWallpaper = iv != 0; break;
                        case "Language": Localizer.Lang = iv; break;
                        case "ScheduledSwitch": ScheduledSwitch = iv != 0; break;
                        case "GlobalHotkey": GlobalHotkey = iv != 0; break;
                        case "DisableHotkeyInFullscreen": DisableHotkeyInFullscreen = iv != 0; break;
                        case "WallpaperStyle": WallpaperStyle = iv; break;
                        case "ToastNotification": ToastNotification = iv != 0; break;
                        case "ToastSound": ToastSound = iv != 0; break;
                        case "FirstRunDone": FirstRunDone = iv != 0; break;
                        case "OriginalWallpaperCaptured": OriginalWallpaperCaptured = iv != 0; break;
                        case "OriginalWallpaperPosition": OriginalWallpaperPosition = iv; break;
                    }
                    // Theme properties: ThemeN_Name, ThemeN_Dark, ThemeN_Light, ThemeN_Style, ThemeN_Enabled
                    for (int i = 0; i < 4; i++)
                    {
                        if (key == $"Theme{i}_Style" && iv is >= 0 and <= 3)
                            Themes[i].WallpaperStyle = iv;
                        else if (key == $"Theme{i}_Enabled")
                            Themes[i].IsEnabled = iv != 0;
                    }
                }
                else
                {
                    switch (key)
                    {
                        case "LightTime": LightTime = val; break;
                        case "DarkTime": DarkTime = val; break;
                        case "HotkeyText": HotkeyText = val; break;
                        case "DarkWallpaper": DarkWallpaper = val; break;
                        case "LightWallpaper": LightWallpaper = val; break;
                        case "OriginalWallpaper": OriginalWallpaper = val; break;
                        case "DashboardLayout": shouldSaveMigratedConfig = true; break;
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        if (key == $"Theme{i}_Name") Themes[i].Name = val;
                        else if (key == $"Theme{i}_Dark") Themes[i].DarkWallpaper = val;
                        else if (key == $"Theme{i}_Light") Themes[i].LightWallpaper = val;
                    }
                }
            }
            // Migrate legacy config: if no Theme0 entries exist but DarkWallpaper does, copy to Theme0
            if (string.IsNullOrEmpty(Themes[0].DarkWallpaper) && !string.IsNullOrEmpty(DarkWallpaper))
            {
                Themes[0].DarkWallpaper = DarkWallpaper;
                Themes[0].LightWallpaper = LightWallpaper;
                Themes[0].WallpaperStyle = WallpaperStyle;
                Themes[0].IsEnabled = AutoSwitchWallpaper;
                Themes[0].Name = "Preset 1";
            }
            if (NormalizeWallpaperPresetState())
                shouldSaveMigratedConfig = true;
            if (SeedPreviewWallpaperPresetIfNeeded())
                shouldSaveMigratedConfig = true;
            if (shouldSaveMigratedConfig)
                Save();
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Settings load failed");
        }
    }

    public void Save()
    {
        lock (_fileLock)
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                var updated = new Dictionary<string, string> {
                    { "SingleClickToggle", SingleClickToggle ? "1" : "0" },
                    { "TrayClickEnabled", TrayClickEnabled ? "1" : "0" },
                    { "Language", Localizer.Lang.ToString() },
                    { "AutoSwitchWallpaper", AutoSwitchWallpaper ? "1" : "0" },
                    { "ScheduledSwitch", ScheduledSwitch ? "1" : "0" },
                    { "LightTime", LightTime },
                    { "DarkTime", DarkTime },
                    { "GlobalHotkey", GlobalHotkey ? "1" : "0" },
                    { "HotkeyText", HotkeyText },
                    { "DisableHotkeyInFullscreen", DisableHotkeyInFullscreen ? "1" : "0" },
                    { "ToastNotification", ToastNotification ? "1" : "0" },
                    { "ToastSound", ToastSound ? "1" : "0" },
                    { "FirstRunDone", FirstRunDone ? "1" : "0" },
                    { "OriginalWallpaperCaptured", OriginalWallpaperCaptured ? "1" : "0" },
                    { "OriginalWallpaper", OriginalWallpaper },
                    { "OriginalWallpaperPosition", OriginalWallpaperPosition.ToString() },
                };
                for (int i = 0; i < 4; i++)
                {
                    var t = Themes[i];
                    updated[$"Theme{i}_Name"] = t.Name;
                    updated[$"Theme{i}_Dark"] = t.DarkWallpaper;
                    updated[$"Theme{i}_Light"] = t.LightWallpaper;
                    updated[$"Theme{i}_Style"] = t.WallpaperStyle.ToString();
                    updated[$"Theme{i}_Enabled"] = t.IsEnabled ? "1" : "0";
                }
                var lines = File.Exists(ConfigPath)
                    ? new List<string>(File.ReadAllLines(ConfigPath))
                    : [];
                var legacyKeys = new HashSet<string> { "DarkWallpaper", "LightWallpaper", "WallpaperStyle", "DashboardLayout", "ScenePresets", "CurrentScenePresetId" };
                var keep = new List<string>();
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#') || !trimmed.Contains('='))
                    { keep.Add(line); continue; }
                    var key = trimmed[..trimmed.IndexOf('=')].Trim();
                    if (legacyKeys.Contains(key)) continue;
                    if (updated.TryGetValue(key, out var val))
                    {
                        keep.Add($"{key}={val}");
                        updated.Remove(key);
                    }
                    else keep.Add(line);
                }
                foreach (var kv in updated)
                    keep.Add($"{kv.Key}={kv.Value}");

                var tmpPath = ConfigPath + ".tmp";
                File.WriteAllText(tmpPath, string.Join('\n', keep) + '\n');
                File.Move(tmpPath, ConfigPath, true);
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex, "Settings save failed");
            }
        }
    }

    public string ExportLink()
    {
        var dict = new Dictionary<string, string>
        {
            ["SCT"] = SingleClickToggle ? "1" : "0",
            ["TCE"] = TrayClickEnabled ? "1" : "0",
            ["LNG"] = Localizer.Lang.ToString(),
            ["SCH"] = ScheduledSwitch ? "1" : "0",
            ["LT"] = LightTime,
            ["DT"] = DarkTime,
            ["GHK"] = GlobalHotkey ? "1" : "0",
            ["HKT"] = HotkeyText,
            ["DHK"] = DisableHotkeyInFullscreen ? "1" : "0",
            ["TN"] = ToastNotification ? "1" : "0",
            ["TS"] = ToastSound ? "1" : "0",
        };
        var text = string.Join('&', dict.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        return "TX1." + Base64Url(GZipCompress(System.Text.Encoding.UTF8.GetBytes(text)));
    }

    public bool ImportFromLink(string link)
    {
        try
        {
            link = link.Trim();
            if (!link.StartsWith("TX1.")) return false;
            var compressed = Base64UrlDecode(link[4..]);
            var text = System.Text.Encoding.UTF8.GetString(GZipDecompress(compressed));
            var dict = new Dictionary<string, string>();
            foreach (var pair in text.Split('&'))
            {
                var eq = pair.IndexOf('=');
                if (eq > 0) dict[pair[..eq]] = Uri.UnescapeDataString(pair[(eq + 1)..]);
            }

            if (dict.TryGetValue("LNG", out var lang) && int.TryParse(lang, out var lv))
                Localizer.Lang = lv;
            if (dict.TryGetValue("SCT", out var sct)) SingleClickToggle = sct == "1";
            if (dict.TryGetValue("TCE", out var tce)) TrayClickEnabled = tce == "1";
            if (dict.TryGetValue("SCH", out var ss)) ScheduledSwitch = ss == "1";
            if (dict.TryGetValue("LT", out var lt)) LightTime = lt;
            if (dict.TryGetValue("DT", out var dt)) DarkTime = dt;
            if (dict.TryGetValue("GHK", out var gh)) GlobalHotkey = gh == "1";
            if (dict.TryGetValue("HKT", out var ht)) HotkeyText = ht;
            if (dict.TryGetValue("DHK", out var dhk)) DisableHotkeyInFullscreen = dhk == "1";
            if (dict.TryGetValue("TN", out var tn)) ToastNotification = tn == "1";
            if (dict.TryGetValue("TS", out var ts)) ToastSound = ts == "1";

            NormalizeWallpaperPresetState();
            Save();
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Settings import token failed");
            return false;
        }
    }

    private static byte[] GZipCompress(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionLevel.SmallestSize))
            gz.Write(data, 0, data.Length);
        return ms.ToArray();
    }

    private static byte[] GZipDecompress(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress);
        using var result = new MemoryStream();
        var buffer = new byte[81920];
        int totalRead = 0;
        int bytesRead;
        while ((bytesRead = gz.Read(buffer, 0, buffer.Length)) > 0)
        {
            totalRead += bytesRead;
            if (totalRead > 1_048_576) throw new InvalidOperationException("Decompressed data exceeds 1MB limit");
            result.Write(buffer, 0, bytesRead);
        }
        return result.ToArray();
    }

    private static string Base64Url(byte[] data)
    {
        return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string text)
    {
        text = text.Replace('-', '+').Replace('_', '/');
        switch (text.Length % 4) { case 2: text += "=="; break; case 3: text += "="; break; }
        return Convert.FromBase64String(text);
    }

    public bool NormalizeWallpaperPresetState()
    {
        var changed = false;
        if (Themes.Length != 4)
        {
            var normalized = new WallpaperTheme[4]
            {
                new() { Name = "1" },
                new() { Name = "" },
                new() { Name = "" },
                new() { Name = "" },
            };
            for (int i = 0; i < Math.Min(Themes.Length, normalized.Length); i++)
                normalized[i] = Themes[i]?.Clone() ?? normalized[i];
            Themes = normalized;
            changed = true;
        }

        var firstWallpaper = -1;
        var firstEnabledWithWallpaper = -1;

        for (int i = 0; i < Themes.Length; i++)
        {
            Themes[i] ??= new WallpaperTheme();
            var theme = Themes[i];

            if (theme.Name == null)
            {
                theme.Name = "";
                changed = true;
            }
            if (theme.DarkWallpaper == null)
            {
                theme.DarkWallpaper = "";
                changed = true;
            }
            if (theme.LightWallpaper == null)
            {
                theme.LightWallpaper = "";
                changed = true;
            }
            if (theme.WallpaperStyle is < 0 or > 3)
            {
                theme.WallpaperStyle = 0;
                changed = true;
            }

            if (!HasWallpaper(theme))
                continue;

            if (firstWallpaper < 0)
                firstWallpaper = i;
            if (firstEnabledWithWallpaper < 0 && theme.IsEnabled)
                firstEnabledWithWallpaper = i;
        }

        if (firstWallpaper < 0)
        {
            if (AutoSwitchWallpaper)
            {
                AutoSwitchWallpaper = false;
                changed = true;
            }
            for (int i = 0; i < Themes.Length; i++)
            {
                if (!Themes[i].IsEnabled) continue;
                Themes[i].IsEnabled = false;
                changed = true;
            }
            return changed;
        }

        var selectedIndex = firstEnabledWithWallpaper >= 0 ? firstEnabledWithWallpaper : firstWallpaper;
        for (int i = 0; i < Themes.Length; i++)
        {
            var shouldEnable = i == selectedIndex;
            if (Themes[i].IsEnabled == shouldEnable) continue;
            Themes[i].IsEnabled = shouldEnable;
            changed = true;
        }

        return changed;
    }

    private static bool HasWallpaper(WallpaperTheme theme) =>
        !string.IsNullOrEmpty(theme.DarkWallpaper) || !string.IsNullOrEmpty(theme.LightWallpaper);

    public bool CaptureOriginalWallpaperIfNeeded()
    {
        if (OriginalWallpaperCaptured)
            return true;

        if (!WallpaperHelper.TryGetCurrentWallpaper(out var wallpaper) || string.IsNullOrWhiteSpace(wallpaper))
            return false;

        OriginalWallpaper = wallpaper;
        OriginalWallpaperPosition = WallpaperHelper.TryGetCurrentWallpaperPosition(out var position)
            ? (int)position
            : -1;
        OriginalWallpaperCaptured = true;
        Save();
        return true;
    }

    public bool RestoreOriginalWallpaper()
    {
        if (!OriginalWallpaperCaptured || string.IsNullOrWhiteSpace(OriginalWallpaper))
            return false;

        if (!WallpaperHelper.SetWallpaperPath(OriginalWallpaper))
            return false;

        if (OriginalWallpaperPosition is >= (int)NativeMethods.DWPosition.Center and <= (int)NativeMethods.DWPosition.Span)
            WallpaperHelper.SetWallpaperPosition((NativeMethods.DWPosition)OriginalWallpaperPosition);

        AutoSwitchWallpaper = false;
        Save();
        return true;
    }

    private bool SeedPreviewWallpaperPresetIfNeeded()
    {
        if (!ProductInfo.IsPreview || Themes.Any(HasWallpaper))
            return false;

        var darkWallpaper = GetBundledWallpaperPath(PreviewDarkWallpaperFileName);
        var lightWallpaper = GetBundledWallpaperPath(PreviewLightWallpaperFileName);
        if (!File.Exists(darkWallpaper) || !File.Exists(lightWallpaper))
        {
            AppLogger.Log($"Preview wallpaper assets missing: {darkWallpaper}; {lightWallpaper}");
            return false;
        }

        Themes[0] = new WallpaperTheme
        {
            Name = PreviewPresetName,
            DarkWallpaper = darkWallpaper,
            LightWallpaper = lightWallpaper,
            WallpaperStyle = PreviewWallpaperStyle,
            IsEnabled = true,
        };

        AutoSwitchWallpaper = false;
        return true;
    }

    private static string GetBundledWallpaperPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Wallpapers", fileName);
}
