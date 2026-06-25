using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Tenlux.Helpers;

internal static class ImageHelper
{
    public static void LoadPreview(Image img, string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) { img.Source = null; return; }
        try
        {
            if (img.Source is BitmapImage old) old.UriSource = null;
            var bmp = new BitmapImage { DecodePixelWidth = 500, UriSource = new Uri(path) };
            img.Source = bmp;
        }
        catch { img.Source = null; }
    }
}
