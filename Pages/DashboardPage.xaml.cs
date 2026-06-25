using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Tenlux.Helpers;
using Windows.Foundation;
using static Tenlux.Helpers.Localizer;

namespace Tenlux.Pages;

public sealed partial class DashboardPage : Page, IReleasablePage
{
    private sealed class DashboardCardViewModel
    {
        public string Id { get; init; } = "";
        public string Title { get; init; } = "";
        public string Summary { get; init; } = "";
        public string IconGlyph { get; init; } = "\uE946";
        public string NavigateTag { get; init; } = "";
        public string? NavigateSection { get; init; }
    }

    private double _screenRatio = 16.0 / 9.0;
    private string? _currentPreviewPath;
    private string? _dashboardGridSignature;
    private static DashboardPage? _instance;
    private SettingsManager Cfg => App.Settings;
    private readonly TypedEventHandler<FrameworkElement, object> _themeChangedHandler;

    public static void RefreshPreviewIfVisible() => _instance?.RefreshAll();
    public static void ClearInstance() => _instance = null;

    public DashboardPage()
    {
        InitializeComponent();
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        _instance = this;
        _themeChangedHandler = OnThemeChanged;
        Unloaded += OnUnloaded;
        ActualThemeChanged += _themeChangedHandler;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        RefreshAll();
    }

    private void RefreshAll()
    {
        ApplyLabels();
        UpdateScreenRatio();
        if (PreviewBorder.ActualWidth > 0)
            PreviewBorder.Height = GetPreviewHeight(PreviewBorder.ActualWidth);
        UpdateStatus();
        LoadCurrentPreview();
        RebuildDashboardGrid();
    }

    public void ApplyLabels()
    {
        TxtQuickSettings.Text = T(S_QuickSettings);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        bool isDark = ActualTheme == ElementTheme.Dark;
        ModeIcon.Glyph = isDark ? "\uE708" : "\uE706";
        TxtMode.Text = isDark ? T(S_DarkMode) : T(S_LightMode);
        BtnToggle.Content = isDark ? T(S_LightMode) : T(S_DarkMode);
    }

    private void OnThemeChanged(FrameworkElement sender, object args)
    {
        UpdateScreenRatio();
        if (PreviewBorder.ActualWidth > 0)
            PreviewBorder.Height = GetPreviewHeight(PreviewBorder.ActualWidth);
        LoadCurrentPreview();
        RebuildDashboardGrid();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _instance = this;
    }

    private void OnPreviewTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        ConnectedAnimationService.GetForCurrentView()
            .PrepareToAnimate("DashboardWallpaperPreview", PreviewBorder);
        SettingsPage.Instance?.NavigateTo("Wallpaper");
    }

    private void OnToggleClick(object _, RoutedEventArgs e)
    {
        MainWindow.Instance.ToggleTheme();
    }

    private void UpdateScreenRatio()
    {
        _screenRatio = NativeMethods.GetStartupScreenAspectRatio();
    }

    private void OnPreviewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Border border && e.NewSize.Width > 0)
            border.Height = GetPreviewHeight(border.ActualWidth);
    }

    private double GetPreviewHeight(double width) => width / _screenRatio;

    private void LoadCurrentPreview()
    {
        bool isLight = ActualTheme == ElementTheme.Light;
        var theme = Cfg.Themes.FirstOrDefault(t => t.IsEnabled && HasWallpaper(t))
            ?? Cfg.Themes.FirstOrDefault(t => t.IsEnabled);
        var path = theme != null
            ? (isLight ? theme.LightWallpaper : theme.DarkWallpaper)
            : null;
        if (string.IsNullOrEmpty(path))
            path = theme?.DarkWallpaper ?? string.Empty;

        if (path == _currentPreviewPath)
            return;

        _currentPreviewPath = path;
        ImageHelper.LoadPreview(ImgWallpaper, path);
    }

    private void RebuildDashboardGrid()
    {
        var cards = BuildDashboardCards().ToList();
        var signature = BuildDashboardGridSignature(cards);
        if (signature == _dashboardGridSignature && DashboardGrid.Children.Count > 0)
            return;

        _dashboardGridSignature = signature;
        DashboardGrid.Children.Clear();
        DashboardGrid.RowDefinitions.Clear();

        int rowCount = (int)Math.Ceiling(cards.Count / 2.0);
        for (int i = 0; i < rowCount; i++)
            DashboardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int i = 0; i < cards.Count; i++)
        {
            var element = CreateDashboardCard(cards[i]);
            Grid.SetRow(element, i / 2);
            Grid.SetColumn(element, i % 2);
            DashboardGrid.Children.Add(element);
        }
    }

    private IEnumerable<DashboardCardViewModel> BuildDashboardCards()
    {
        yield return CreateStatusCard("current-preset", T(S_CurrentPreset), BuildPresetSummary(), "\uE14C", "Wallpaper");
        yield return CreateSettingCard("wallpaper-link", T(S_HealthWallpaper), BuildWallpaperSummary(), "\uE91B", "Wallpaper");
        yield return CreateSettingCard("global-hotkey", T(S_GlobalHotkey), BuildHotkeySummary(), "\uE765", "Hotkey", "HotkeyExpand");
        yield return CreateSettingCard("scheduled-switch", T(S_HealthSchedule), BuildScheduleSummary(), "\uE823", "Hotkey", "Schedule");
        yield return CreateSettingCard("tray-click", T(S_HealthTray), BuildTraySummary(), "\uE7F4", "Hotkey", "TrayClick");
        yield return CreateSettingCard("startup", T(S_HealthStartup), BuildStartupSummary(), "\uE895", "General");
    }

    private static DashboardCardViewModel CreateSettingCard(string id, string title, string summary, string iconGlyph, string navigateTag, string? navigateSection = null)
    {
        return new DashboardCardViewModel
        {
            Id = id,
            Title = title,
            Summary = summary,
            IconGlyph = iconGlyph,
            NavigateTag = navigateTag,
            NavigateSection = navigateSection,
        };
    }

    private static DashboardCardViewModel CreateStatusCard(string id, string title, string summary, string iconGlyph, string navigateTag)
    {
        return new DashboardCardViewModel
        {
            Id = id,
            Title = title,
            Summary = summary,
            IconGlyph = iconGlyph,
            NavigateTag = navigateTag,
        };
    }

    private FrameworkElement CreateDashboardCard(DashboardCardViewModel card)
    {
        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(14, 12, 14, 12),
            MinHeight = 76,
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Tag = card.Id,
        };

        var root = new Grid
        {
            ColumnSpacing = 10,
            RowSpacing = 4,
        };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = new FontIcon
        {
            Glyph = card.IconGlyph,
            FontSize = 16,
            Opacity = 0.72,
            VerticalAlignment = VerticalAlignment.Center,
        };
        root.Children.Add(icon);

        var summaryText = string.IsNullOrWhiteSpace(card.Summary) ? T(S_NoPreset) : card.Summary;
        var title = new TextBlock
        {
            Text = $"{card.Title} / {summaryText}",
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 2,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(title, 1);
        root.Children.Add(title);

        var arrow = new FontIcon
        {
            Glyph = "\uE76C",
            FontSize = 12,
            Opacity = 0.5,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(arrow, 2);
        root.Children.Add(arrow);

        button.Content = root;
        MotionHelper.AddCardLift(button);
        button.Click += (_, _) => NavigateFromCard(card);
        AutomationProperties.SetAutomationId(button, $"DashboardCard_{card.Id}");
        AutomationProperties.SetName(button, $"{card.Title} {summaryText}");
        return button;
    }

    private void NavigateFromCard(DashboardCardViewModel card)
    {
        if (card.NavigateSection != null)
            SettingsPage.Instance?.NavigateTo(card.NavigateSection);
        else
            SettingsPage.Instance?.NavigateTo(card.NavigateTag);
    }

    private string BuildWallpaperSummary()
    {
        return Cfg.AutoSwitchWallpaper ? T(S_On) : T(S_Off);
    }

    private string BuildTraySummary()
    {
        if (!Cfg.TrayClickEnabled)
            return T(S_Off);
        return Cfg.SingleClickToggle ? T(S_SingleClick) : T(S_DoubleClick);
    }

    private string BuildHotkeySummary()
    {
        if (!Cfg.GlobalHotkey || string.IsNullOrWhiteSpace(Cfg.HotkeyText))
            return T(S_Off);
        return Cfg.HotkeyText;
    }

    private string BuildScheduleSummary()
    {
        if (!Cfg.ScheduledSwitch)
            return T(S_Off);
        return $"{Cfg.DarkTime} - {Cfg.LightTime}";
    }

    private string BuildStartupSummary() => StartupHelper.IsStartupEnabled() ? T(S_On) : T(S_Off);

    private string BuildPresetSummary()
    {
        var active = Cfg.Themes.FirstOrDefault(t => t.IsEnabled);
        return string.IsNullOrWhiteSpace(active?.Name) ? T(S_NoPreset) : active!.Name;
    }

    private static bool HasWallpaper(WallpaperTheme theme) =>
        !string.IsNullOrEmpty(theme.DarkWallpaper) || !string.IsNullOrEmpty(theme.LightWallpaper);

    public void ReleaseResources()
    {
        _currentPreviewPath = null;
        _dashboardGridSignature = null;
        UiCleanupHelper.ReleaseImage(ImgWallpaper);
        DashboardGrid.Children.Clear();
        DashboardGrid.RowDefinitions.Clear();
        _instance = null;
    }

    private static string BuildDashboardGridSignature(IEnumerable<DashboardCardViewModel> cards)
    {
        return string.Join('\u001f', cards.Select(card =>
            string.Join('\u001e',
                Localizer.Lang,
                card.Id,
                card.Title,
                card.Summary,
                card.IconGlyph,
                card.NavigateTag,
                card.NavigateSection ?? string.Empty)));
    }
}
