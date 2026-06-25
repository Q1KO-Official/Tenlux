using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Tenlux.Helpers;

internal interface IReleasablePage
{
    void ReleaseResources();
}

internal static class UiCleanupHelper
{
    public static void ReleaseFrame(Frame? frame)
    {
        if (frame == null) return;

        if (frame.Content is IReleasablePage releasable)
            releasable.ReleaseResources();

        frame.BackStack.Clear();
        frame.Content = null;
        frame.CacheSize = 0;
    }

    public static void ReleaseImage(Image? image)
    {
        if (image?.Source is BitmapImage bitmap)
            bitmap.UriSource = null;

        if (image != null)
            image.Source = null;
    }
}
