using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Tenlux.Helpers;
using Windows.ApplicationModel.DataTransfer;

namespace Tenlux.Pages;

public sealed partial class GeneralPage : Page
{
    private static SettingsManager Cfg => App.Settings;
    private bool _suppress;
    private bool _loaded;

    public GeneralPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Enabled;
        Loaded += OnLoaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (_loaded)
            RefreshFromSettings();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) { RefreshLabels(); return; }
        _loaded = true;

        _suppress = true;
        ChkStartup.IsOn = StartupHelper.IsStartupEnabled();
        Localizer.PopulateLangCombo(CmbLang);
        _suppress = false;
        ApplyLabels();
    }

    private void RefreshLabels()
    {
        _suppress = true;
        Localizer.RefreshLangCombo(CmbLang);
        _suppress = false;
        ApplyLabels();
    }

    private void ApplyLabels()
    {
        CardStartup.Header = Localizer.T(Localizer.S_Startup);
        CardLang.Header = Localizer.T(Localizer.S_Language);
        ChkStartup.OnContent = Localizer.T(Localizer.S_On);
        ChkStartup.OffContent = Localizer.T(Localizer.S_Off);
        CardMigration.Header = Localizer.T(Localizer.S_ConfigMigration);
        BtnExport.Content = Localizer.T(Localizer.S_Export);
        MenuExportToken.Text = Localizer.T(Localizer.S_ConfigToken);
        MenuExportFile.Text = Localizer.T(Localizer.S_ConfigFileWP);
        BtnImport.Content = Localizer.T(Localizer.S_Import);
        MenuImportToken.Text = Localizer.T(Localizer.S_ConfigToken);
        MenuImportFile.Text = Localizer.T(Localizer.S_ConfigFileWP);
        CardReset.Header = Localizer.T(Localizer.S_ResetSettings);
        BtnResetSettings.Content = Localizer.T(Localizer.S_ResetSettings);
    }

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        var link = Cfg.ExportLink();
        var package = new DataPackage();
        package.SetText(link);
        Clipboard.SetContent(package);
        ShowToast(Localizer.T(Localizer.S_ExportDone));
    }

    private async void OnExportFileClick(object sender, RoutedEventArgs e)
    {
        try
        {
#if DEBUG
            var testPath = Environment.GetEnvironmentVariable("TENLUX_TEST_EXPORT_FILE_PATH");
            if (!string.IsNullOrWhiteSpace(testPath))
            {
                await ExportConfigFileAsync(testPath);
                ShowToast(Localizer.T(Localizer.S_ExportFileDone));
                return;
            }
#endif

            var picker = new Microsoft.Windows.Storage.Pickers.FileSavePicker(MainWindow.Instance.AppWindow.Id);
            picker.SuggestedStartLocation = Microsoft.Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = GetSuggestedExportFileName();
            picker.FileTypeChoices.Add($"{ProductInfo.Name} Config", [".tx"]);
            var result = await picker.PickSaveFileAsync();
            if (result == null) return;

            await ExportConfigFileAsync(result.Path);
            ShowToast(Localizer.T(Localizer.S_ExportFileDone));
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message);
        }
    }

    private static Task ExportConfigFileAsync(string path)
    {
        return Task.Run(() =>
        {
            var tempPath = Path.Combine(
                Path.GetDirectoryName(path) ?? Path.GetTempPath(),
                $"{Path.GetFileNameWithoutExtension(path)}.{Guid.NewGuid():N}.tmp");

            try
            {
                using var zip = System.IO.Compression.ZipFile.Open(tempPath, System.IO.Compression.ZipArchiveMode.Create);

                var configEntry = zip.CreateEntry("config.txt");
                using (var writer = new StreamWriter(configEntry.Open()))
                {
                    writer.WriteLine($"Language={Localizer.Lang}");
                    writer.WriteLine($"SingleClickToggle={(Cfg.SingleClickToggle ? 1 : 0)}");
                    writer.WriteLine($"TrayClickEnabled={(Cfg.TrayClickEnabled ? 1 : 0)}");
                    writer.WriteLine($"AutoSwitchWallpaper={(Cfg.AutoSwitchWallpaper ? 1 : 0)}");
                    writer.WriteLine($"ScheduledSwitch={(Cfg.ScheduledSwitch ? 1 : 0)}");
                    writer.WriteLine($"LightTime={Cfg.LightTime}");
                    writer.WriteLine($"DarkTime={Cfg.DarkTime}");
                    writer.WriteLine($"GlobalHotkey={(Cfg.GlobalHotkey ? 1 : 0)}");
                    writer.WriteLine($"HotkeyText={Cfg.HotkeyText}");
                    writer.WriteLine($"DisableHotkeyInFullscreen={(Cfg.DisableHotkeyInFullscreen ? 1 : 0)}");
                    writer.WriteLine($"ToastNotification={(Cfg.ToastNotification ? 1 : 0)}");
                    writer.WriteLine($"ToastSound={(Cfg.ToastSound ? 1 : 0)}");
                    for (int i = 0; i < 4; i++)
                    {
                        var t = Cfg.Themes[i];
                        writer.WriteLine($"Theme{i}_Name={t.Name}");
                        writer.WriteLine($"Theme{i}_Style={t.WallpaperStyle}");
                        writer.WriteLine($"Theme{i}_Enabled={(t.IsEnabled ? 1 : 0)}");
                        if (!string.IsNullOrEmpty(t.DarkWallpaper) && File.Exists(t.DarkWallpaper))
                            writer.WriteLine($"Theme{i}_Dark=wallpapers/{i}_dark{Path.GetExtension(t.DarkWallpaper)}");
                        if (!string.IsNullOrEmpty(t.LightWallpaper) && File.Exists(t.LightWallpaper))
                            writer.WriteLine($"Theme{i}_Light=wallpapers/{i}_light{Path.GetExtension(t.LightWallpaper)}");
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    var t = Cfg.Themes[i];
                    if (!string.IsNullOrEmpty(t.DarkWallpaper) && File.Exists(t.DarkWallpaper))
                    {
                        var entry = zip.CreateEntry($"wallpapers/{i}_dark{Path.GetExtension(t.DarkWallpaper)}");
                        using var src = File.OpenRead(t.DarkWallpaper);
                        using var dst = entry.Open();
                        src.CopyTo(dst);
                    }
                    if (!string.IsNullOrEmpty(t.LightWallpaper) && File.Exists(t.LightWallpaper))
                    {
                        var entry = zip.CreateEntry($"wallpapers/{i}_light{Path.GetExtension(t.LightWallpaper)}");
                        using var src = File.OpenRead(t.LightWallpaper);
                        using var dst = entry.Open();
                        src.CopyTo(dst);
                    }
                }

                zip.Dispose();
                File.Move(tempPath, path, true);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); }
                catch { }
            }
        });
    }

    private static string GetSuggestedExportFileName()
    {
        try
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrWhiteSpace(documents))
            {
                var path = GetAvailableExportPath(Path.Combine(documents, $"{ProductInfo.Name}.tx"));
                return Path.GetFileNameWithoutExtension(path);
            }
        }
        catch
        {
            // Fall back to the product name if Documents cannot be resolved.
        }

        return ProductInfo.Name;
    }

    private static string GetAvailableExportPath(string requestedPath)
    {
        var directory = Path.GetDirectoryName(requestedPath);
        if (string.IsNullOrWhiteSpace(directory))
            directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        var extension = Path.GetExtension(requestedPath);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".tx";

        var originalName = Path.GetFileNameWithoutExtension(requestedPath);
        if (string.IsNullOrWhiteSpace(originalName))
            originalName = ProductInfo.Name;

        var firstCandidate = Path.Combine(directory, originalName + extension);
        if (!File.Exists(firstCandidate))
            return firstCandidate;

        var prefix = originalName;
        var firstNumber = 1;
        var digitStart = originalName.Length;
        while (digitStart > 0 && char.IsDigit(originalName[digitStart - 1]))
            digitStart--;

        if (digitStart > 0 && digitStart < originalName.Length)
        {
            prefix = originalName[..digitStart];
            if (int.TryParse(originalName[digitStart..], out var parsed))
                firstNumber = parsed + 1;
        }

        for (var number = firstNumber; number < 10000; number++)
        {
            var candidate = Path.Combine(directory, $"{prefix}{number}{extension}");
            if (!File.Exists(candidate))
                return candidate;
        }

        return Path.Combine(directory, $"{prefix}{DateTime.Now:yyyyMMddHHmmss}{extension}");
    }

    private async void OnImportClick(object sender, RoutedEventArgs e)
    {
        var input = new TextBox { PlaceholderText = "TX1...", Width = 400 };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(input, "GeneralImportTokenTextBox");
        var dlg = new ContentDialog
        {
            Title = Localizer.T(Localizer.S_ImportConfig),
            Content = input,
            PrimaryButtonText = Localizer.T(Localizer.S_ImportConfig),
            CloseButtonText = Localizer.T(Localizer.S_Back),
            XamlRoot = XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;
        var text = input.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        var ok = Cfg.ImportFromLink(text);
        ShowToast(ok ? Localizer.T(Localizer.S_ImportDone) : Localizer.T(Localizer.S_ImportFail));
        if (ok)
        {
            MainWindow.Instance.RegisterGlobalHotkey();
            MainWindow.Instance.UpdateScheduleTimer();
            MainWindow.Instance.SyncWallpaperForCurrentTheme();
            SettingsPage.Instance?.ApplyNavLabels();
            WallpaperOverviewPage.RefreshPreviewIfVisible();
            DashboardPage.RefreshPreviewIfVisible();
            RefreshFromSettings();
        }
    }

    private void ShowToast(string message)
    {
        ToastHelper.ShowToast(message, false);
    }

    private async void OnImportFileClick(object sender, RoutedEventArgs e)
    {
        try
        {
            string? path = null;
#if DEBUG
            path = Environment.GetEnvironmentVariable("TENLUX_TEST_IMPORT_FILE_PATH");
#endif
            if (string.IsNullOrWhiteSpace(path))
            {
                var picker = new Microsoft.Windows.Storage.Pickers.FileOpenPicker(MainWindow.Instance.AppWindow.Id)
                {
                    SuggestedStartLocation = Microsoft.Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                };
                picker.FileTypeFilter.Add(".tx");
                var file = await picker.PickSingleFileAsync();
                if (file == null) return;
                path = file.Path;
            }

            // Background: extract zip and copy wallpaper files
            var result = await Task.Run(() =>
            {
                using var zip = System.IO.Compression.ZipFile.OpenRead(path);
                var configEntry = zip.GetEntry("config.txt");
                if (configEntry == null) return (dict: (Dictionary<string, string>?)null, paths: new Dictionary<string, string>());
                using var reader = new StreamReader(configEntry.Open());
                var dict = new Dictionary<string, string>();
                while (reader.ReadLine() is { } line)
                {
                    var eq = line.IndexOf('=');
                    if (eq > 0) dict[line[..eq]] = line[(eq + 1)..];
                }

                var wallpaperDir = SettingsManager.WallpaperDir;
                Directory.CreateDirectory(wallpaperDir);
                var copiedPaths = new Dictionary<string, string>();

                for (int i = 0; i < 4; i++)
                {
                    if (dict.TryGetValue($"Theme{i}_Dark", out var darkRef) && darkRef.StartsWith("wallpapers/"))
                    {
                        var entry = zip.GetEntry(darkRef);
                        if (entry != null)
                        {
                            var destPath = Path.Combine(wallpaperDir, $"{i}_dark{Path.GetExtension(darkRef)}");
                            using var src = entry.Open();
                            using var dst = File.Create(destPath);
                            src.CopyTo(dst);
                            copiedPaths[$"Theme{i}_Dark"] = destPath;
                        }
                    }
                    if (dict.TryGetValue($"Theme{i}_Light", out var lightRef) && lightRef.StartsWith("wallpapers/"))
                    {
                        var entry = zip.GetEntry(lightRef);
                        if (entry != null)
                        {
                            var destPath = Path.Combine(wallpaperDir, $"{i}_light{Path.GetExtension(lightRef)}");
                            using var src = entry.Open();
                            using var dst = File.Create(destPath);
                            src.CopyTo(dst);
                            copiedPaths[$"Theme{i}_Light"] = destPath;
                        }
                    }
                }

                return (dict, paths: copiedPaths);
            });

            if (result.dict == null) return;

            // UI thread: apply config to SettingsManager
            var d = result.dict;
            if (d.TryGetValue("Language", out var lang) && int.TryParse(lang, out var lv))
                Localizer.Lang = lv;
            if (d.TryGetValue("SingleClickToggle", out var sct)) Cfg.SingleClickToggle = sct == "1";
            if (d.TryGetValue("TrayClickEnabled", out var tce)) Cfg.TrayClickEnabled = tce == "1";
            if (d.TryGetValue("AutoSwitchWallpaper", out var asw)) Cfg.AutoSwitchWallpaper = asw == "1";
            if (d.TryGetValue("ScheduledSwitch", out var ss)) Cfg.ScheduledSwitch = ss == "1";
            if (d.TryGetValue("LightTime", out var lt)) Cfg.LightTime = lt;
            if (d.TryGetValue("DarkTime", out var dt)) Cfg.DarkTime = dt;
            if (d.TryGetValue("GlobalHotkey", out var gh)) Cfg.GlobalHotkey = gh == "1";
            if (d.TryGetValue("HotkeyText", out var ht)) Cfg.HotkeyText = ht;
            if (d.TryGetValue("DisableHotkeyInFullscreen", out var dhk)) Cfg.DisableHotkeyInFullscreen = dhk == "1";
            if (d.TryGetValue("ToastNotification", out var tn)) Cfg.ToastNotification = tn == "1";
            if (d.TryGetValue("ToastSound", out var ts)) Cfg.ToastSound = ts == "1";

            for (int i = 0; i < 4; i++)
            {
                var theme = Cfg.Themes[i];
                theme.Name = d.TryGetValue($"Theme{i}_Name", out var n) ? n : (i == 0 ? "1" : "");
                theme.WallpaperStyle = d.TryGetValue($"Theme{i}_Style", out var sty) && int.TryParse(sty, out var sv) && sv is >= 0 and <= 3
                    ? sv
                    : 0;
                theme.IsEnabled = d.TryGetValue($"Theme{i}_Enabled", out var en) && en == "1";
                theme.DarkWallpaper = result.paths.TryGetValue($"Theme{i}_Dark", out var dp) ? dp : "";
                theme.LightWallpaper = result.paths.TryGetValue($"Theme{i}_Light", out var lp) ? lp : "";
            }

            Cfg.NormalizeWallpaperPresetState();
            Cfg.Save();
            ShowToast(Localizer.T(Localizer.S_ImportDone));
            MainWindow.Instance.RegisterGlobalHotkey();
            MainWindow.Instance.UpdateScheduleTimer();
            MainWindow.Instance.SyncWallpaperForCurrentTheme();
            SettingsPage.Instance?.ApplyNavLabels();
            WallpaperOverviewPage.RefreshPreviewIfVisible();
            DashboardPage.RefreshPreviewIfVisible();
            RefreshLabels();
            RefreshFromSettings();
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message);
        }
    }

    private void OnStartupChanged(object _, RoutedEventArgs e)
    {
        if (_suppress) return;
        var path = Environment.ProcessPath;
        if (string.IsNullOrEmpty(path)) return;
        StartupHelper.SetStartupEnabled(ChkStartup.IsOn, path);
    }

    private void OnLangChanged(object _, SelectionChangedEventArgs e)
    {
        if (_suppress || CmbLang.SelectedIndex < 0 || CmbLang.SelectedIndex == Localizer.Lang) return;
        Localizer.Lang = CmbLang.SelectedIndex;
        Cfg.Save();
        RefreshLabels();
        // Update sidebar and app title
        SettingsPage.Instance?.ApplyNavLabels();
        MainWindow.Instance.Title = Localizer.T(Localizer.S_AppName);
    }

    private async void OnResetSettingsClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = Localizer.T(Localizer.S_ResetSettings),
            Content = Localizer.T(Localizer.S_ResetSettingsConfirm),
            PrimaryButtonText = Localizer.T(Localizer.S_ResetSettings),
            CloseButtonText = Localizer.T(Localizer.S_Back),
            XamlRoot = XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        Cfg.ResetToDefaults();
        var path = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(path))
            StartupHelper.SetStartupEnabled(false, path);
        MainWindow.Instance.RegisterGlobalHotkey();
        MainWindow.Instance.UpdateScheduleTimer();
        MainWindow.Instance.SyncWallpaperForCurrentTheme();
        DashboardPage.RefreshPreviewIfVisible();
        WallpaperOverviewPage.RefreshPreviewIfVisible();

        _suppress = true;
        ChkStartup.IsOn = StartupHelper.IsStartupEnabled();
        Localizer.RefreshLangCombo(CmbLang);
        _suppress = false;
        RefreshFromSettings();
        ShowToast(Localizer.T(Localizer.S_ResetSettingsDone));
    }

    private void RefreshFromSettings()
    {
        _suppress = true;
        ChkStartup.IsOn = StartupHelper.IsStartupEnabled();
        Localizer.RefreshLangCombo(CmbLang);
        _suppress = false;
        ApplyLabels();
    }

}
