using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Tenlux.Helpers;
using static Tenlux.Helpers.ImageHelper;

namespace Tenlux.Pages;

public sealed partial class WallpaperEditPage : Page, IReleasablePage
{
    private static SettingsManager Cfg => App.Settings;
    private bool _suppress;
    private int _themeIndex;

    public WallpaperEditPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is int idx && idx >= 0 && idx < 4)
            _themeIndex = idx;
        else
            DispatcherQueue.TryEnqueue(() => Frame.GoBack());
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Auto-name empty presets
        if (string.IsNullOrEmpty(Cfg.Themes[_themeIndex].Name))
        {
            Cfg.Themes[_themeIndex].Name = $"{_themeIndex + 1}";
            Cfg.Save();
        }

        var t = Cfg.Themes[_themeIndex];

        CmbWallStyle.Items.Clear();
        CmbWallStyle.Items.Add(Localizer.T(Localizer.S_Fill));
        CmbWallStyle.Items.Add(Localizer.T(Localizer.S_Fit));
        CmbWallStyle.Items.Add(Localizer.T(Localizer.S_Stretch));
        CmbWallStyle.Items.Add(Localizer.T(Localizer.S_Tile));

        _suppress = true;
        TxtPresetName.Text = t.Name;
        PathDark.Text = t.DarkWallpaper;
        PathLight.Text = t.LightWallpaper;
        CmbWallStyle.SelectedIndex = t.WallpaperStyle;
        _suppress = false;

        LoadPreview(PicDark, t.DarkWallpaper);
        LoadPreview(PicLight, t.LightWallpaper);
        ApplyLabels();
    }

    private void ApplyLabels()
    {
        BtnBack.Content = "← " + Localizer.T(Localizer.S_Back);
        CardDarkWall.Header = Localizer.T(Localizer.S_DarkWallpaper);
        CardLightWall.Header = Localizer.T(Localizer.S_LightWallpaper);
        CardWallStyle.Header = Localizer.T(Localizer.S_WallpaperDisplay);
        BtnBrowseDark.Content = Localizer.T(Localizer.S_Browse);
        BtnBrowseLight.Content = Localizer.T(Localizer.S_Browse);
        BtnSave.Content = Localizer.T(Localizer.S_Save);
        CardPresetName.Header = Localizer.T(Localizer.S_PresetName);
    }

    private void OnSaveClick(object _, RoutedEventArgs e)
    {
        Cfg.Themes[_themeIndex].Name = TxtPresetName.Text;
        Cfg.Save();
        DashboardPage.RefreshPreviewIfVisible();
        NavigateBack();
    }

    private void OnBackClick(object _, RoutedEventArgs e)
    {
        NavigateBack();
    }

    private void NavigateBack()
    {
        var parentFrame = this.Parent as Frame;
        parentFrame?.Navigate(typeof(WallpaperOverviewPage));
    }

    private void OnDarkPathChanged(object _, TextChangedEventArgs e)
    {
        if (_suppress) return;
        Cfg.Themes[_themeIndex].DarkWallpaper = PathDark.Text;
        Cfg.SaveDebounced();
        DashboardPage.RefreshPreviewIfVisible();
        SyncDesktopWallpaperIfCurrentPreset();
        LoadPreview(PicDark, PathDark.Text);
    }

    private void OnLightPathChanged(object _, TextChangedEventArgs e)
    {
        if (_suppress) return;
        Cfg.Themes[_themeIndex].LightWallpaper = PathLight.Text;
        Cfg.SaveDebounced();
        DashboardPage.RefreshPreviewIfVisible();
        SyncDesktopWallpaperIfCurrentPreset();
        LoadPreview(PicLight, PathLight.Text);
    }

    private void OnWallStyleChanged(object _, SelectionChangedEventArgs e)
    {
        if (_suppress || CmbWallStyle.SelectedIndex < 0) return;
        Cfg.Themes[_themeIndex].WallpaperStyle = CmbWallStyle.SelectedIndex;
        Cfg.Save();
        DashboardPage.RefreshPreviewIfVisible();
        if (Cfg.Themes[_themeIndex].IsEnabled && Cfg.AutoSwitchWallpaper)
        {
            Cfg.CaptureOriginalWallpaperIfNeeded();
            WallpaperHelper.SetWallpaperPosition(Cfg.Themes[_themeIndex].WallpaperStyle);
        }
    }

    private void OnPresetNameChanged(object _, TextChangedEventArgs e)
    {
        if (_suppress) return;
        Cfg.Themes[_themeIndex].Name = TxtPresetName.Text;
        Cfg.SaveDebounced();
        DashboardPage.RefreshPreviewIfVisible();
    }

    private async void OnBrowseDarkClick(object _, RoutedEventArgs e)
    {
        string? file = null;
#if DEBUG
        file = Environment.GetEnvironmentVariable("TENLUX_TEST_DARK_WALLPAPER_PATH");
#endif
        if (string.IsNullOrWhiteSpace(file))
            file = await PickImageFile();
        if (file == null) return;
        _suppress = true;
        Cfg.Themes[_themeIndex].DarkWallpaper = file;
        PathDark.Text = file;
        _suppress = false;
        LoadPreview(PicDark, file);
        Cfg.Save();
        DashboardPage.RefreshPreviewIfVisible();
        SyncDesktopWallpaperIfCurrentPreset();
    }

    private async void OnBrowseLightClick(object _, RoutedEventArgs e)
    {
        string? file = null;
#if DEBUG
        file = Environment.GetEnvironmentVariable("TENLUX_TEST_LIGHT_WALLPAPER_PATH");
#endif
        if (string.IsNullOrWhiteSpace(file))
            file = await PickImageFile();
        if (file == null) return;
        _suppress = true;
        Cfg.Themes[_themeIndex].LightWallpaper = file;
        PathLight.Text = file;
        _suppress = false;
        LoadPreview(PicLight, file);
        Cfg.Save();
        DashboardPage.RefreshPreviewIfVisible();
        SyncDesktopWallpaperIfCurrentPreset();
    }

    private static async Task<string?> PickImageFile()
    {
        try
        {
            var picker = new Microsoft.Windows.Storage.Pickers.FileOpenPicker(MainWindow.Instance.AppWindow.Id)
            {
                SuggestedStartLocation = Microsoft.Windows.Storage.Pickers.PickerLocationId.PicturesLibrary,
            };
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".bmp");
            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }
        catch (Exception ex)
        {
            var dlg = new ContentDialog { Title = Localizer.T(Localizer.S_Error), Content = ex.Message, CloseButtonText = Localizer.T(Localizer.S_OK) };
            dlg.XamlRoot = MainWindow.Instance.Content.XamlRoot;
            await dlg.ShowAsync();
            return null;
        }
    }

    private void SyncDesktopWallpaperIfCurrentPreset()
    {
        if (Cfg.Themes[_themeIndex].IsEnabled && Cfg.AutoSwitchWallpaper)
            MainWindow.Instance.SyncWallpaperForCurrentTheme();
    }

    public void ReleaseResources()
    {
        UiCleanupHelper.ReleaseImage(PicDark);
        UiCleanupHelper.ReleaseImage(PicLight);
    }
}
