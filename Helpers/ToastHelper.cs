using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Tenlux.Helpers;

internal static class ToastHelper
{
    private const string AppId = "Tenlux";
    private const string ToastTag = "Tenlux_ThemeSwitch";

    private static ToastNotifier? _notifier;
    private static ToastNotification? _currentToast;

    public static void Release()
    {
        _notifier = null;
        _currentToast = null;
    }

    public static void ShowToast(string message, bool playSound)
    {
        try
        {
            _notifier ??= ToastNotificationManager.CreateToastNotifier(AppId);

            // Hide previous toast immediately
            if (_currentToast != null)
            {
                try { _notifier.Hide(_currentToast); } catch { }
                _currentToast = null;
            }

            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
            var textNodes = toastXml.GetElementsByTagName("text");
            if (textNodes.Length > 0)
            {
                textNodes[0].AppendChild(toastXml.CreateTextNode(message));
            }

            // Only modify audio when sound should be silent
            if (!playSound)
            {
                var toastNode = toastXml.SelectSingleNode("/toast");
                if (toastNode != null)
                {
                    var audio = toastXml.CreateElement("audio");
                    audio.SetAttribute("silent", "true");
                    toastNode.AppendChild(audio);
                }
            }

            var toast = new ToastNotification(toastXml) { Tag = ToastTag };
            _currentToast = toast;
            _notifier.Show(toast);
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "Toast show failed");
            _currentToast = null;
        }
    }
}
