using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Navigation;
using System.IO.Compression;
using Tenlux.Helpers;

namespace Tenlux.Pages;

public sealed partial class AboutPage : Page
{
    private bool _loaded;

    public AboutPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Enabled;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) { ApplyLabels(); return; }
        _loaded = true;

        TxtVersion.Text = ProductInfo.Version;
        TxtLicenseBody.Text = GetLicenseText();
        ApplyLabels();
    }

    private void ApplyLabels()
    {
        TxtBrief.Text = Localizer.T(Localizer.S_AboutBrief);
        TxtAppName.Text = Localizer.T(Localizer.S_AppName);
        CardVersion.Header = Localizer.T(Localizer.S_AboutVersion);
        CardSource.Header = Localizer.T(Localizer.S_AboutProjectSource);
        LnkGitHub.Content = ProductInfo.RepositoryUrl.Replace("https://", "");
        CardDeveloper.Header = Localizer.T(Localizer.S_AboutDeveloper);
        TxtLicenseHeader.Text = Localizer.T(Localizer.S_AboutLicense);
        TxtCopyright.Text = Localizer.T(Localizer.S_AboutCopyright);
        CardTutorial.Header = Localizer.T(Localizer.S_ViewTutorial);
        BtnTutorial.Content = Localizer.T(Localizer.S_OpenTutorial);
        AutomationProperties.SetName(BtnTutorial, Localizer.T(Localizer.S_OpenTutorial));
        CardLogs.Header = Localizer.T(Localizer.S_Logs);
        BtnExportLogs.Content = Localizer.T(Localizer.S_ExportLogs);
        AutomationProperties.SetName(BtnExportLogs, Localizer.T(Localizer.S_ExportLogs));
        CardOriginalWallpaper.Header = Localizer.T(Localizer.S_OriginalWallpaper);
        BtnRestoreWallpaper.Content = Localizer.T(Localizer.S_RestoreWallpaper);
        AutomationProperties.SetName(BtnRestoreWallpaper, Localizer.T(Localizer.S_RestoreWallpaper));
    }

    private void OnTutorialClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.ShowOnboarding();
    }

    private async void OnExportLogsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new Microsoft.Windows.Storage.Pickers.FileSavePicker(MainWindow.Instance.AppWindow.Id);
            picker.SuggestedStartLocation = Microsoft.Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = $"{ProductInfo.Name}-logs-{DateTime.Now:yyyyMMdd-HHmmss}";
            picker.FileTypeChoices.Add($"{ProductInfo.Name} Logs", [".zip"]);
            var result = await picker.PickSaveFileAsync();
            if (result == null) return;

            await Task.Run(() => ExportLogPackage(result.Path));
            ToastHelper.ShowToast(Localizer.T(Localizer.S_LogExportDone), false);
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Log export failed");
            await ShowErrorAsync(ex.Message);
        }
    }

    private async void OnRestoreWallpaperClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!App.Settings.RestoreOriginalWallpaper())
            {
                await ShowErrorAsync(Localizer.T(Localizer.S_RestoreWallpaperUnavailable));
                return;
            }

            ToastHelper.ShowToast(Localizer.T(Localizer.S_RestoreWallpaperDone), false);
            DashboardPage.RefreshPreviewIfVisible();
            WallpaperOverviewPage.RefreshPreviewIfVisible();
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Original wallpaper restore failed");
            await ShowErrorAsync(ex.Message);
        }
    }

    private static void ExportLogPackage(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
            directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        var tempPath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var zip = ZipFile.Open(tempPath, ZipArchiveMode.Create))
            {
                AddFileIfExists(zip, AppLogger.CurrentLogPath, $"{ProductInfo.Name}.log");
                AddFileIfExists(zip, AppLogger.CurrentLogPath + ".1", $"{ProductInfo.Name}.log.1");
                AddFileIfExists(zip, SettingsManager.CurrentConfigPath, $"{ProductInfo.Name}.cfg");

                var diagnostics = zip.CreateEntry("diagnostics.txt");
                using var writer = new StreamWriter(diagnostics.Open());
                writer.WriteLine($"Product={ProductInfo.Name}");
                writer.WriteLine($"Version={ProductInfo.Version}");
                writer.WriteLine($"ExportedAt={DateTimeOffset.Now:O}");
                writer.WriteLine($"OS={Environment.OSVersion}");
                writer.WriteLine($"ProcessArchitecture={System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
                writer.WriteLine($"Framework={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
                writer.WriteLine($"ConfigPath={SettingsManager.CurrentConfigPath}");
                writer.WriteLine($"LogPath={AppLogger.CurrentLogPath}");
            }

            File.Move(tempPath, path, true);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
            }
        }
    }

    private static void AddFileIfExists(ZipArchive zip, string sourcePath, string entryName)
    {
        if (File.Exists(sourcePath))
            zip.CreateEntryFromFile(sourcePath, entryName, CompressionLevel.Optimal);
    }

    private async Task ShowErrorAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = Localizer.T(Localizer.S_Error),
            Content = message,
            CloseButtonText = Localizer.T(Localizer.S_OK),
            XamlRoot = XamlRoot,
        };
        await dialog.ShowAsync();
    }

    private static string GetLicenseText() =>
        """
        CC BY-NC-SA 4.0

        Copyright (c) 2026 Q1KO

        You are free to:
          Share - copy and redistribute the material in any medium or format
          Adapt - remix, transform, and build upon the material

        Under the following terms:
          Attribution - You must give appropriate credit.
          NonCommercial - You may not use the material for commercial purposes.
          ShareAlike - Derivatives must use the same license.

        Full license: creativecommons.org/licenses/by-nc-sa/4.0
        """;
}
