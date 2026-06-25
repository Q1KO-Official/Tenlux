using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Tenlux.Helpers;

namespace Tenlux.Pages;

public sealed partial class WallpaperPage : Page, IReleasablePage
{
    private const int WallpaperContentCacheSize = 2;

    public WallpaperPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Enabled;
        SubFrame.CacheSize = WallpaperContentCacheSize;
        SubFrame.ContentTransitions = new TransitionCollection
        {
            new NavigationThemeTransition
            {
                DefaultNavigationTransitionInfo = new DrillInNavigationTransitionInfo()
            }
        };
        NavigateToOverviewIfNeeded();
        Loaded += (_, _) =>
        {
            NavigateToOverviewIfNeeded();
        };
    }

    private void NavigateToOverviewIfNeeded()
    {
        if (SubFrame.Content == null)
            SubFrame.Navigate(typeof(WallpaperOverviewPage));
    }

    public void ReleaseResources()
    {
        UiCleanupHelper.ReleaseFrame(SubFrame);
    }
}
