using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Tenlux.Helpers;

internal static class MotionHelper
{
    public static void AddChildrenEntrance(Panel panel, double offset = 16)
    {
        panel.ChildrenTransitions = new TransitionCollection
        {
            new EntranceThemeTransition
            {
                FromVerticalOffset = offset,
                IsStaggeringEnabled = true,
            },
            new RepositionThemeTransition(),
            new AddDeleteThemeTransition(),
        };
    }

    public static void AddChildrenReposition(Panel panel)
    {
        panel.ChildrenTransitions = new TransitionCollection
        {
            new RepositionThemeTransition(),
            new AddDeleteThemeTransition(),
        };
    }

    public static void AddCardLift(UIElement element, double hoverScale = 1.01, double pressedScale = 0.985)
    {
        if (element is FrameworkElement frameworkElement)
            frameworkElement.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);

        var transform = new CompositeTransform();
        element.RenderTransform = transform;

        element.PointerEntered += (_, _) => AnimateTransform(transform, hoverScale, hoverScale, -2, 120);
        element.PointerExited += (_, _) => AnimateTransform(transform, 1, 1, 0, 140);
        element.PointerPressed += (_, _) => AnimateTransform(transform, pressedScale, pressedScale, 0, 70);
        element.PointerReleased += (_, _) => AnimateTransform(transform, hoverScale, hoverScale, -2, 90);
        element.PointerCanceled += (_, _) => AnimateTransform(transform, 1, 1, 0, 120);
    }

    public static void AddFadeIn(UIElement element, double from = 0, double to = 1, int durationMs = 160)
    {
        element.Opacity = from;
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private static void AnimateTransform(CompositeTransform transform, double scaleX, double scaleY, double translateY, int durationMs)
    {
        var storyboard = new Storyboard();
        AddAnimation(storyboard, transform, "ScaleX", scaleX, durationMs);
        AddAnimation(storyboard, transform, "ScaleY", scaleY, durationMs);
        AddAnimation(storyboard, transform, "TranslateY", translateY, durationMs);
        storyboard.Begin();
    }

    private static void AddAnimation(Storyboard storyboard, DependencyObject target, string property, double to, int durationMs)
    {
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        storyboard.Children.Add(animation);
    }
}
