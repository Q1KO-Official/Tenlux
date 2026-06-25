using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Tenlux.Helpers;

namespace Tenlux.Pages;

public sealed partial class SettingsPage : Page, IReleasablePage
{
    private const int SettingsContentCacheSize = 5;
    private NavigationViewItem? _dashboardItem, _generalItem, _hotkeyItem, _wallpaperItem, _aboutItem;
    private string? _pendingNavParam;
    private FrameworkElement? _pendingReadyElement;
    public static string? PendingLaunchTag { get; set; }

    public static SettingsPage? Instance { get; private set; }
    public static void ClearInstance() => Instance = null;

    public SettingsPage()
    {
        Instance = this;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MainWindow.Instance.SetTitleBar(AppTitleBar);
        MainWindow.Instance.RefreshWindowChrome();

        ContentFrame.CacheSize = SettingsContentCacheSize;
        ContentFrame.ContentTransitions = CreatePageTransitions();

        if (Nav.MenuItems.Count == 0)
        {
            _dashboardItem = new NavigationViewItem
            {
                Tag = "Dashboard",
                Icon = new FontIcon { Glyph = "" }
            };
            _generalItem = new NavigationViewItem
            {
                Tag = "General",
                Icon = new FontIcon { Glyph = "" }
            };
            _hotkeyItem = new NavigationViewItem
            {
                Tag = "Hotkey",
                Icon = new FontIcon { Glyph = "" }
            };
            _wallpaperItem = new NavigationViewItem
            {
                Tag = "Wallpaper",
                Icon = new FontIcon { Glyph = "" }
            };
            Nav.MenuItems.Add(_dashboardItem);
            Nav.MenuItems.Add(_generalItem);
            Nav.MenuItems.Add(_hotkeyItem);
            Nav.MenuItems.Add(_wallpaperItem);
            AutomationProperties.SetAutomationId(_dashboardItem, "SettingsNavDashboardItem");
            AutomationProperties.SetAutomationId(_generalItem, "SettingsNavGeneralItem");
            AutomationProperties.SetAutomationId(_hotkeyItem, "SettingsNavHotkeyItem");
            AutomationProperties.SetAutomationId(_wallpaperItem, "SettingsNavWallpaperItem");
            Nav.SelectedItem = _dashboardItem;
        }

        if (Nav.FooterMenuItems.Count == 0)
        {
            _aboutItem = new NavigationViewItem
            {
                Tag = "About",
                Icon = new FontIcon { Glyph = "" }
            };
            Nav.FooterMenuItems.Add(_aboutItem);
            AutomationProperties.SetAutomationId(_aboutItem, "SettingsNavAboutItem");
        }
        ApplyNavLabels();

        if (!string.IsNullOrWhiteSpace(PendingLaunchTag))
        {
            var pending = PendingLaunchTag;
            PendingLaunchTag = null;
            DispatcherQueue.TryEnqueue(() => NavigateTo(pending));
        }

    }

    public void ApplyNavLabels()
    {
        TitleText.Text = Localizer.T(Localizer.S_AppName);
        SetNavLabel(_dashboardItem, Localizer.T(Localizer.S_NavDashboard));
        SetNavLabel(_generalItem, Localizer.T(Localizer.S_NavGeneral));
        SetNavLabel(_hotkeyItem, Localizer.T(Localizer.S_NavHotkey));
        SetNavLabel(_wallpaperItem, Localizer.T(Localizer.S_NavWallpaper));
        SetNavLabel(_aboutItem, Localizer.T(Localizer.S_NavAbout));
    }

    private static void SetNavLabel(NavigationViewItem? item, string label)
    {
        if (item == null)
            return;

        item.Content = label;
        AutomationProperties.SetName(item, label);
    }

    public void ClearPageCache()
    {
        UiCleanupHelper.ReleaseFrame(ContentFrame);
        Nav.MenuItems.Clear();
        Nav.FooterMenuItems.Clear();
        _dashboardItem = _generalItem = _hotkeyItem = _wallpaperItem = _aboutItem = null;
        PendingLaunchTag = null;
    }

    public void ReleaseResources()
    {
        ClearPageCache();
    }

    public void NavigateTo(string tag)
    {
        var navTag = tag switch
        {
            "HotkeyExpand" or "Schedule" or "Toast" or "TrayClick" => "Hotkey",
            _ => tag
        };
        var parameter = tag switch
        {
            "HotkeyExpand" => "Hotkey",
            "Schedule" or "Toast" or "TrayClick" => tag,
            _ => null
        };

        var item = Nav.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (string)i.Tag == navTag)
                   ?? Nav.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (string)i.Tag == navTag);
        if (item == null) return;

        if (ReferenceEquals(Nav.SelectedItem, item) && ContentFrame.Content is HotkeyPage existingPage)
        {
            // Already on HotkeyPage — just expand the section
            if (parameter != null) existingPage.ExpandSection(parameter);
        }
        else
        {
            _pendingNavParam = parameter;
            Nav.SelectedItem = item;
        }
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItem is not NavigationViewItem item) return;

        var pageType = item.Tag switch
        {
            "Dashboard" => typeof(DashboardPage),
            "General" => typeof(GeneralPage),
            "Hotkey" => typeof(HotkeyPage),
            "Wallpaper" => typeof(WallpaperPage),
            "About" => typeof(AboutPage),
            _ => null
        };
        if (pageType != null)
        {
            if (_pendingReadyElement != null)
            {
                _pendingReadyElement.Loaded -= OnContentPageLoaded;
                _pendingReadyElement = null;
            }

            if (ContentFrame.CurrentSourcePageType == pageType)
            {
                if (_pendingNavParam != null && ContentFrame.Content is HotkeyPage hotkeyPage)
                    hotkeyPage.ExpandSection(_pendingNavParam);
            }
            else
            {
                ContentFrame.ContentTransitions = CreatePageTransitions();
                ContentFrame.Navigate(pageType, _pendingNavParam, new DrillInNavigationTransitionInfo());
            }

            ContentFrame.BackStack.Clear();
            ContentFrame.CacheSize = SettingsContentCacheSize;
            _pendingNavParam = null;

            if (ContentFrame.Content is FrameworkElement element)
                WatchContentPageReady(element);
        }
    }

    private void OnContentPageLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
            element.Loaded -= OnContentPageLoaded;

        _pendingReadyElement = null;
        DispatcherQueue.TryEnqueue(() =>
        {
            CompositionTarget.Rendering += OnContentPageRendered;
        });
    }

    private void WatchContentPageReady(FrameworkElement element)
    {
        if (element.IsLoaded)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                CompositionTarget.Rendering += OnContentPageRendered;
            });
            return;
        }

        _pendingReadyElement = element;
        element.Loaded += OnContentPageLoaded;
    }

    private void OnContentPageRendered(object? sender, object e)
    {
        CompositionTarget.Rendering -= OnContentPageRendered;
        MainWindow.Instance.QueueSettingsContentReady();
    }

    private static TransitionCollection CreatePageTransitions()
    {
        return new TransitionCollection
        {
            new NavigationThemeTransition
            {
                DefaultNavigationTransitionInfo = new DrillInNavigationTransitionInfo()
            }
        };
    }
}
