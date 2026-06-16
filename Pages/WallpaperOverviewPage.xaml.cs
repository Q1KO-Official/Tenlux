using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Tenlux.Helpers;
using Windows.Foundation;
using static Tenlux.Helpers.ImageHelper;

namespace Tenlux.Pages;

public sealed partial class WallpaperOverviewPage : Page, IReleasablePage
{
    private static SettingsManager Cfg => App.Settings;
    private bool _suppress;
    private double _screenRatio = 16.0 / 9.0;

    private static WallpaperOverviewPage? _instance;
    public static void RefreshPreviewIfVisible() => _instance?.LoadCurrentPreview(animate: false);
    public static void ClearInstance() => _instance = null;
    private string? _currentPreviewPath;
    private string? _presetGridSignature;

    private static readonly SolidColorBrush _brushTransparent = new(Colors.Transparent);
    private static readonly SolidColorBrush _brushBlack60 = new(Colors.Black) { Opacity = 0.6 };
    private static readonly SolidColorBrush _brushBlack50 = new(Colors.Black) { Opacity = 0.5 };
    private static readonly SolidColorBrush _brushWhite = new(Colors.White);
    private static readonly SolidColorBrush _brushGray = new(Colors.Gray);
    private static readonly SolidColorBrush _brushGray15 = new(Colors.Gray) { Opacity = 0.15 };
    private static readonly SolidColorBrush _brushGray25 = new(Colors.Gray) { Opacity = 0.25 };
    private static readonly SolidColorBrush _brushRed80 = new(Colors.Red) { Opacity = 0.8 };
    private readonly TypedEventHandler<FrameworkElement, object> _themeChangedHandler;

    public WallpaperOverviewPage()
    {
        _instance = this;
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Enabled;
        MotionHelper.AddChildrenEntrance(PresetGrid);
        _themeChangedHandler = OnThemeChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ActualThemeChanged += _themeChangedHandler;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _suppress = true;
        ChkAutoWall.IsOn = Cfg.AutoSwitchWallpaper;
        _suppress = false;
        UpdateScreenRatio();
        if (PreviewBorder.ActualWidth > 0)
            PreviewBorder.Height = GetPreviewHeight(PreviewBorder.ActualWidth);
        LoadCurrentPreview(animate: false);
        RebuildPresetGrid();
    }

    private void UpdateScreenRatio()
    {
        _screenRatio = NativeMethods.GetStartupScreenAspectRatio();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _instance = this;
        ApplyLabels();
        TryStartConnectedPreviewAnimation();
    }

    private void ApplyLabels()
    {
        CardAutoWall.Header = Localizer.T(Localizer.S_AutoSwitchWallpaper);
        LblPresets.Text = Localizer.T(Localizer.S_Presets);
        ChkAutoWall.OnContent = Localizer.T(Localizer.S_On);
        ChkAutoWall.OffContent = Localizer.T(Localizer.S_Off);
    }

    private void TryStartConnectedPreviewAnimation()
    {
        ConnectedAnimationService.GetForCurrentView()
            .GetAnimation("DashboardWallpaperPreview")
            ?.TryStart(PreviewBorder);
    }

    private void OnThemeChanged(FrameworkElement sender, object args)
    {
        UpdateScreenRatio();
        if (PreviewBorder.ActualWidth > 0)
            PreviewBorder.Height = GetPreviewHeight(PreviewBorder.ActualWidth);
        LoadCurrentPreview();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _instance = this;
    }

    private static bool HasWallpaper(WallpaperTheme theme) =>
        !string.IsNullOrEmpty(theme.DarkWallpaper) || !string.IsNullOrEmpty(theme.LightWallpaper);

    private int FindEnabledPresetIndex()
    {
        for (int i = 0; i < Cfg.Themes.Length; i++)
        {
            if (Cfg.Themes[i].IsEnabled && HasWallpaper(Cfg.Themes[i]))
                return i;
        }

        return -1;
    }

    private int FindFirstPresetIndex()
    {
        for (int i = 0; i < Cfg.Themes.Length; i++)
        {
            if (HasWallpaper(Cfg.Themes[i]))
                return i;
        }

        return -1;
    }

    private void ApplyPresetToDesktop(int index)
    {
        if (index < 0 || index >= Cfg.Themes.Length)
            return;

        var theme = Cfg.Themes[index];
        var isLight = ThemeHelper.ReadCurrentThemeIsLight();
        var dark = theme.DarkWallpaper;
        var light = theme.LightWallpaper;
        var style = theme.WallpaperStyle;

        ThreadPool.QueueUserWorkItem(_ =>
        {
            Cfg.CaptureOriginalWallpaperIfNeeded();
            WallpaperHelper.SetWallpaper(isLight, dark, light);
            WallpaperHelper.SetWallpaperPosition(style);
        });
    }

    private async Task PromptCreatePresetAsync()
    {
        var dialog = new ContentDialog
        {
            Title = Localizer.T(Localizer.S_AutoSwitchWallpaper),
            Content = Localizer.T(Localizer.S_AddPresetFirst),
            PrimaryButtonText = Localizer.T(Localizer.S_AddPreset),
            CloseButtonText = Localizer.T(Localizer.S_Cancel),
            XamlRoot = XamlRoot,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            NavigateToEdit(0);
    }

    private void LoadCurrentPreview(bool animate = true)
    {
        bool isLight = ActualTheme == Microsoft.UI.Xaml.ElementTheme.Light;
        var theme = Cfg.Themes.FirstOrDefault(t => t.IsEnabled && HasWallpaper(t))
            ?? Cfg.Themes.FirstOrDefault(t => t.IsEnabled);
        var path = theme != null
            ? (isLight ? theme.LightWallpaper : theme.DarkWallpaper)
            : null;
        if (string.IsNullOrEmpty(path)) path = theme?.DarkWallpaper ?? "";

        // Skip if already showing this image
        if (path == _currentPreviewPath) return;
        _currentPreviewPath = path;

        if (!animate)
        {
            LoadPreview(CurrentPreview, path);
            return;
        }

        // Fade out → swap image → fade in
        var fadeOut = new DoubleAnimation
        {
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(fadeOut, CurrentPreview);
        Storyboard.SetTargetProperty(fadeOut, "Opacity");
        var sb = new Storyboard();
        sb.Children.Add(fadeOut);
        sb.Completed += (_, _) =>
        {
            if (!IsLoaded) return;
            LoadPreview(CurrentPreview, path);
            var fadeIn = new DoubleAnimation
            {
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(250)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, CurrentPreview);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");
            var sb2 = new Storyboard();
            sb2.Children.Add(fadeIn);
            sb2.Begin();
        };
        sb.Begin();
    }

    private int GetActiveCount()
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            var t = Cfg.Themes[i];
            if (HasWallpaper(t))
                count++;
        }
        return count;
    }

    private void RebuildPresetGrid()
    {
        var signature = BuildPresetGridSignature();
        if (signature == _presetGridSignature && PresetGrid.Children.Count > 0)
            return;

        _presetGridSignature = signature;
        var grid = PresetGrid;
        grid.Children.Clear();
        grid.RowDefinitions.Clear();

        var presetIndexes = GetPresetIndexes().ToList();
        var addIndex = GetFirstEmptyPresetIndex();
        int totalSlots = presetIndexes.Count + (addIndex >= 0 ? 1 : 0);
        int rows = Math.Max(1, (totalSlots + 1) / 2);

        for (int r = 0; r < rows; r++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int i = 0; i < totalSlots; i++)
        {
            int col = i % 2;
            int row = i / 2;

            if (i < presetIndexes.Count)
            {
                var card = CreatePresetCard(presetIndexes[i]);
                Grid.SetColumn(card, col);
                Grid.SetRow(card, row);
                grid.Children.Add(card);
            }
            else if (addIndex >= 0)
            {
                var addCard = CreateAddCard(addIndex);
                Grid.SetColumn(addCard, col);
                Grid.SetRow(addCard, row);
                grid.Children.Add(addCard);
            }
        }
    }

    private void NavigateToEdit(int index)
    {
        (Parent as Frame)?.Navigate(typeof(WallpaperEditPage), index);
    }

    private IEnumerable<int> GetPresetIndexes()
    {
        for (int i = 0; i < Cfg.Themes.Length; i++)
        {
            if (HasWallpaper(Cfg.Themes[i]))
                yield return i;
        }
    }

    private int GetFirstEmptyPresetIndex()
    {
        for (int i = 0; i < Cfg.Themes.Length; i++)
        {
            if (!HasWallpaper(Cfg.Themes[i]))
                return i;
        }

        return -1;
    }

    private Grid CreatePresetCard(int index)
    {
        var t = Cfg.Themes[index];
        bool isEmpty = string.IsNullOrEmpty(t.DarkWallpaper) && string.IsNullOrEmpty(t.LightWallpaper);

        if (isEmpty)
        {
            var preset = CreateAddCard(index);
            var nameTag = new Border
            {
                Background = _brushBlack60,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(6, 6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            nameTag.Child = new TextBlock
            {
                Text = t.Name,
                Foreground = _brushWhite,
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            };
            preset.Children.Add(nameTag);
            return preset;
        }
        var card = new Grid { CornerRadius = new CornerRadius(8), Tag = index, Background = _brushTransparent };
        MotionHelper.AddCardLift(card);
        AutomationProperties.SetAutomationId(card, $"WallpaperPresetCard_{index}");
        AutomationProperties.SetName(card, t.Name);

        // Both images stacked in the same position
        var imgLight = new Image { Stretch = Stretch.UniformToFill };
        LoadPreview(imgLight, t.LightWallpaper);
        card.Children.Add(imgLight);

        var imgDark = new Image { Stretch = Stretch.UniformToFill };
        LoadPreview(imgDark, t.DarkWallpaper);
        card.Children.Add(imgDark);

        card.SizeChanged += (_, e) =>
        {
            if (e.NewSize.Width <= 0) return;
            card.Height = e.NewSize.Width / _screenRatio;
            double half = e.NewSize.Width / 2;
            double h = e.NewSize.Height;
            imgLight.Clip = new RectangleGeometry { Rect = new Rect(0, 0, half, h) };
            imgDark.Clip = new RectangleGeometry { Rect = new Rect(half, 0, half, h) };
        };

        // Preset name (top-left)
        var nameBorder = new Border
        {
            Background = _brushBlack60,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(6, 6, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        Grid.SetColumnSpan(nameBorder, 2);
        nameBorder.Child = new TextBlock
        {
            Text = t.Name,
            Foreground = _brushWhite,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        };
        card.Children.Add(nameBorder);

        // Hover overlay (non-interactive, visual only)
        var overlay = new Border
        {
            Background = _brushBlack50,
            CornerRadius = new CornerRadius(8),
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = false,
        };
        Grid.SetColumnSpan(overlay, 2);

        // Edit text (center, part of overlay)
        var editText = new TextBlock
        {
            Text = Localizer.T(Localizer.S_Edit),
            Foreground = _brushWhite,
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        overlay.Child = editText;
        card.Children.Add(overlay);

        // Apply + Delete buttons (top-right, shown on hover)
        var applyBtn = new Button
        {
            Content = t.IsEnabled ? Localizer.T(Localizer.S_Current) : Localizer.T(Localizer.S_Apply),
            Padding = new Thickness(8, 2, 8, 2),
            Tag = index,
            IsEnabled = !t.IsEnabled,
        };
        applyBtn.Click += OnApplyClick;
        applyBtn.AddHandler(PointerPressedEvent, new PointerEventHandler((s, e) => e.Handled = true), true);
        AutomationProperties.SetAutomationId(applyBtn, $"WallpaperApplyPresetButton_{index}");
        AutomationProperties.SetName(applyBtn, applyBtn.Content?.ToString() ?? Localizer.T(Localizer.S_Apply));

        var deleteBtn = new Button
        {
            Content = Localizer.T(Localizer.S_Delete),
            Background = _brushRed80,
            Foreground = _brushWhite,
            Padding = new Thickness(8, 2, 8, 2),
            Tag = index,
        };
        deleteBtn.Click += OnDeleteClick;
        deleteBtn.AddHandler(PointerPressedEvent, new PointerEventHandler((s, e) => e.Handled = true), true);
        AutomationProperties.SetAutomationId(deleteBtn, $"WallpaperDeletePresetButton_{index}");
        AutomationProperties.SetName(deleteBtn, Localizer.T(Localizer.S_Delete));

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 6, 6, 0),
            Visibility = Visibility.Collapsed,
            Children = { applyBtn, deleteBtn },
        };
        Grid.SetColumnSpan(btnPanel, 2);
        card.Children.Add(btnPanel);

        card.PointerEntered += (_, _) => { overlay.Visibility = Visibility.Visible; btnPanel.Visibility = Visibility.Visible; };
        card.PointerExited += (_, _) => { overlay.Visibility = Visibility.Collapsed; btnPanel.Visibility = Visibility.Collapsed; };
        card.PointerPressed += (s, e) =>
        {
            if (e.GetCurrentPoint(card).Properties.IsLeftButtonPressed)
                NavigateToEdit(index);
        };

        return card;
    }

    private Grid CreateAddCard(int index)
    {
        var card = new Grid
        {
            CornerRadius = new CornerRadius(8),
            Background = _brushGray15,
            Tag = index,
        };
        MotionHelper.AddCardLift(card);
        AutomationProperties.SetAutomationId(card, $"WallpaperAddPresetCard_{index}");
        AutomationProperties.SetName(card, Localizer.T(Localizer.S_AddPreset));

        card.SizeChanged += (s, e) =>
        {
            if (e.NewSize.Width > 0)
                card.Height = e.NewSize.Width / _screenRatio;
        };

        var tb = new TextBlock
        {
            Text = "+",
            FontSize = 36,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = _brushGray,
        };
        card.Children.Add(tb);

        card.PointerEntered += (_, _) => card.Background = _brushGray25;
        card.PointerExited += (_, _) => card.Background = _brushGray15;
        card.PointerPressed += (_, _) => NavigateToEdit(index);

        return card;
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int idx)
            DeletePreset(idx);
    }

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int idx) return;
        for (int i = 0; i < 4; i++)
            Cfg.Themes[i].IsEnabled = i == idx;
        Cfg.Save();
        if (Cfg.AutoSwitchWallpaper)
            ApplyPresetToDesktop(idx);
        LoadCurrentPreview();
        DashboardPage.RefreshPreviewIfVisible();
        RebuildPresetGrid();
    }

    private void DeletePreset(int index)
    {
        var deletedWasEnabled = Cfg.Themes[index].IsEnabled;

        // Shift presets forward to fill the gap
        for (int i = index; i < 3; i++)
        {
            Cfg.Themes[i] = Cfg.Themes[i + 1].Clone();
        }
        Cfg.Themes[3] = new WallpaperTheme { Name = "" };

        // Auto-name only presets that have no custom name (empty or just a digit)
        int count = GetActiveCount();
        for (int i = 0; i < count; i++)
        {
            var name = Cfg.Themes[i].Name;
            if (string.IsNullOrEmpty(name) || (name.Length == 1 && char.IsDigit(name[0])))
                Cfg.Themes[i].Name = $"{i + 1}";
        }

        // Ensure at least one preset is enabled if any have wallpapers
        if (count > 0 && !Cfg.Themes.Any(t => t.IsEnabled))
            Cfg.Themes[0].IsEnabled = true;

        Cfg.Save();
        if (deletedWasEnabled && count > 0 && Cfg.AutoSwitchWallpaper)
            ApplyPresetToDesktop(FindEnabledPresetIndex());

        LoadCurrentPreview();
        DashboardPage.RefreshPreviewIfVisible();
        RebuildPresetGrid();
    }

    private async void OnAutoWallChanged(object _, RoutedEventArgs e)
    {
        if (_suppress) return;

        if (ChkAutoWall.IsOn)
        {
            var presetIndex = FindEnabledPresetIndex();
            if (presetIndex < 0)
                presetIndex = FindFirstPresetIndex();

            if (presetIndex < 0)
            {
                _suppress = true;
                ChkAutoWall.IsOn = false;
                _suppress = false;
                Cfg.AutoSwitchWallpaper = false;
                Cfg.Save();
                DashboardPage.RefreshPreviewIfVisible();
                await PromptCreatePresetAsync();
                return;
            }

            for (int i = 0; i < 4; i++)
                Cfg.Themes[i].IsEnabled = i == presetIndex;
            Cfg.AutoSwitchWallpaper = true;
            Cfg.Save();
            ApplyPresetToDesktop(presetIndex);
        }
        else
        {
            Cfg.AutoSwitchWallpaper = false;
            Cfg.Save();
        }

        LoadCurrentPreview();
        DashboardPage.RefreshPreviewIfVisible();
        RebuildPresetGrid();
    }

    private void OnPreviewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Border border && e.NewSize.Width > 0)
            border.Height = GetPreviewHeight(e.NewSize.Width);
    }

    private double GetPreviewHeight(double width) => width / _screenRatio;

    public void ReleaseResources()
    {
        _currentPreviewPath = null;
        _presetGridSignature = null;
        UiCleanupHelper.ReleaseImage(CurrentPreview);
        PresetGrid.Children.Clear();
        PresetGrid.RowDefinitions.Clear();
        _instance = null;
    }

    private static string BuildPresetGridSignature()
    {
        return string.Join('\u001f', Cfg.Themes.Select(t =>
            string.Join('\u001e',
                Localizer.Lang,
                t.Name,
                t.DarkWallpaper,
                t.LightWallpaper,
                t.WallpaperStyle,
                t.IsEnabled)));
    }
}
