using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Automation;
using Tenlux.Helpers;
using Windows.System;
using static Tenlux.Helpers.NativeMethods;

namespace Tenlux.Pages;

public sealed partial class HotkeyPage : Page
{
    private static SettingsManager Cfg => App.Settings;
    private bool _suppress;
    private bool _loaded;
    private bool _confirmingSingleKey;

    public HotkeyPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Enabled;
        Loaded += OnLoaded;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (_loaded)
            RefreshFromSettings();
        if (e.Parameter is string section)
            ExpandSection(section);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) { RefreshFromSettings(); return; }
        _loaded = true;

        _suppress = true;
        CmbMode.Items.Add(Localizer.T(Localizer.S_SingleClick));
        CmbMode.Items.Add(Localizer.T(Localizer.S_DoubleClick));
        _suppress = false;
        RefreshFromSettings();
        ApplyLabels();
#if DEBUG
        AddAutomationTimeInputsIfNeeded();
#endif
        MainWindow.Instance.RegisterGlobalHotkey();
    }

    private void RefreshFromSettings()
    {
        _suppress = true;
        ChkTrayClick.IsOn = Cfg.TrayClickEnabled;
        CmbMode.SelectedIndex = Cfg.SingleClickToggle ? 0 : 1;
        ChkHotkey.IsOn = Cfg.GlobalHotkey;
        TxtHotkey.Text = Cfg.HotkeyText;
        ChkSchedule.IsOn = Cfg.ScheduledSwitch;
        SetPickerTime(PickerOn, Cfg.DarkTime);
        SetPickerTime(PickerOff, Cfg.LightTime);
        ChkToastNotification.IsOn = Cfg.ToastNotification;
        ChkToastSound.IsOn = Cfg.ToastSound;
        ChkFullscreen.IsOn = Cfg.DisableHotkeyInFullscreen;
        _suppress = false;
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        var modeIdx = CmbMode.SelectedIndex;
        _suppress = true;
        CmbMode.Items[0] = Localizer.T(Localizer.S_SingleClick);
        CmbMode.Items[1] = Localizer.T(Localizer.S_DoubleClick);
        CmbMode.SelectedIndex = modeIdx >= 0 ? modeIdx : (Cfg.SingleClickToggle ? 0 : 1);
        _suppress = false;
        ApplyLabels();
    }

    private void ApplyLabels()
    {
        CardTrayClick.Header = Localizer.T(Localizer.S_TrayToggle);
        CardHotkey.Header = Localizer.T(Localizer.S_GlobalHotkey);
        CardFullscreen.Header = Localizer.T(Localizer.S_DisableHotkeyFullscreen);
        CardSchedule.Header = Localizer.T(Localizer.S_ScheduledSwitch);
        CardTimeOn.Header = Localizer.T(Localizer.S_ScheduleOn);
        CardTimeOff.Header = Localizer.T(Localizer.S_ScheduleOff);
        CardToast.Header = Localizer.T(Localizer.S_ToastTitle);
        CardToastNotification.Header = Localizer.T(Localizer.S_ToastNotification);
        CardToastSound.Header = Localizer.T(Localizer.S_ToastSound);
        var on = Localizer.T(Localizer.S_On);
        var off = Localizer.T(Localizer.S_Off);
        ChkTrayClick.OnContent = on; ChkTrayClick.OffContent = off;
        ChkHotkey.OnContent = on; ChkHotkey.OffContent = off;
        ChkFullscreen.OnContent = on; ChkFullscreen.OffContent = off;
        ChkSchedule.OnContent = on; ChkSchedule.OffContent = off;
        ChkToastNotification.OnContent = on; ChkToastNotification.OffContent = off;
        ChkToastSound.OnContent = on; ChkToastSound.OffContent = off;
        TxtHotkey.PlaceholderText = Localizer.T(Localizer.S_PressHotkey);
    }

    public void ExpandSection(string section)
    {
        CardTrayClick.IsExpanded = section == "TrayClick";
        CardHotkey.IsExpanded = section == "Hotkey";
        CardSchedule.IsExpanded = section == "Schedule";
        CardToast.IsExpanded = section == "Toast";
    }

    private void OnTrayClickChanged(object _, RoutedEventArgs e)
    {
        if (_suppress) return;
        Cfg.TrayClickEnabled = ChkTrayClick.IsOn;
        Cfg.Save();
        DashboardPage.RefreshPreviewIfVisible();
    }

    private void OnModeChanged(object _, SelectionChangedEventArgs e)
    {
        if (_suppress || CmbMode.SelectedIndex < 0) return;
        Cfg.SingleClickToggle = CmbMode.SelectedIndex == 0;
        Cfg.Save();
        DashboardPage.RefreshPreviewIfVisible();
    }

    private void OnHotkeyChanged(object _, RoutedEventArgs e)
    {
        if (_suppress) return;
        Cfg.GlobalHotkey = ChkHotkey.IsOn;
        Cfg.Save();
        MainWindow.Instance.RegisterGlobalHotkey();
        DashboardPage.RefreshPreviewIfVisible();
    }

    private void OnFullscreenChanged(object _, RoutedEventArgs e)
    {
        if (_suppress) return;
        Cfg.DisableHotkeyInFullscreen = ChkFullscreen.IsOn;
        Cfg.Save();
        DashboardPage.RefreshPreviewIfVisible();
    }

    private void OnHotkeyGotFocus(object _, RoutedEventArgs e)
    {
        TxtHotkey.SelectAll();
    }

#if DEBUG
    private async void OnHotkeyTextChangedForAutomation(object _, TextChangedEventArgs e)
    {
        if (_suppress || Environment.GetEnvironmentVariable("TENLUX_TEST_ENABLE_TEXT_HOTKEY_INPUT") != "1")
            return;

        var text = TxtHotkey.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text) || text == Cfg.HotkeyText)
            return;

        if (!HotkeyTextHasModifier(text) && !IsFunctionKeyText(text) && !await ConfirmSingleKeyHotkeyAsync(text))
        {
            _suppress = true;
            TxtHotkey.Text = Cfg.HotkeyText;
            TxtHotkey.SelectAll();
            _suppress = false;
            return;
        }

        ApplyHotkeyText(text);
    }
#else
    private void OnHotkeyTextChangedForAutomation(object _, TextChangedEventArgs e)
    {
    }
#endif

#if DEBUG
    private void AddAutomationTimeInputsIfNeeded()
    {
        if (Environment.GetEnvironmentVariable("TENLUX_TEST_ENABLE_TIME_INPUT") != "1")
            return;

        AddAutomationTimeInput("HotkeyScheduleDarkTimeAutomationTextBox", ApplyDarkTime);
        AddAutomationTimeInput("HotkeyScheduleLightTimeAutomationTextBox", ApplyLightTime);
    }

    private void AddAutomationTimeInput(string automationId, Action<string> apply)
    {
        if (RootPanel.Children.OfType<TextBox>().Any(t => AutomationProperties.GetAutomationId(t) == automationId))
            return;

        var input = new TextBox
        {
            Width = 1,
            Height = 1,
            Opacity = 0,
            IsTabStop = false,
        };
        AutomationProperties.SetAutomationId(input, automationId);
        input.TextChanged += (_, _) =>
        {
            var text = input.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                apply(text);
        };
        RootPanel.Children.Add(input);
    }
#endif

    private async void OnHotkeyKeyDown(object _, KeyRoutedEventArgs e)
    {
        e.Handled = true;
        if (_confirmingSingleKey)
            return;

        var key = e.Key;

        if (key is VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl
            or VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu
            or VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift
            or VirtualKey.LeftWindows or VirtualKey.RightWindows)
            return;

        var ctrl = (GetAsyncKeyState(0x11) & 0x8000) != 0;
        var alt = (GetAsyncKeyState(0x12) & 0x8000) != 0;
        var shift = (GetAsyncKeyState(0x10) & 0x8000) != 0;
        var win = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
        var hasModifier = ctrl || alt || shift || win;

        var parts = new List<string>();
        if (ctrl) parts.Add("Ctrl");
        if (alt) parts.Add("Alt");
        if (shift) parts.Add("Shift");
        if (win) parts.Add("Win");

        var keyName = GetHotkeyKeyName(key);
        parts.Add(keyName);

        var text = string.Join(" + ", parts);

        if (!hasModifier && !IsFunctionKey(key) && !await ConfirmSingleKeyHotkeyAsync(text))
        {
            TxtHotkey.Text = Cfg.HotkeyText;
            TxtHotkey.SelectAll();
            return;
        }

        ApplyHotkeyText(text);
    }

    private static bool HotkeyTextHasModifier(string text)
    {
        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(part => part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)
            || part.Equals("Control", StringComparison.OrdinalIgnoreCase)
            || part.Equals("Alt", StringComparison.OrdinalIgnoreCase)
            || part.Equals("Shift", StringComparison.OrdinalIgnoreCase)
            || part.Equals("Win", StringComparison.OrdinalIgnoreCase)
            || part.Equals("Super", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsFunctionKeyText(string text)
    {
        var key = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? text;
        return key.Length >= 2
            && key[0] is 'F' or 'f'
            && int.TryParse(key[1..], out var fn)
            && fn is >= 1 and <= 24;
    }

    private void ApplyHotkeyText(string text)
    {
        _suppress = true;
        TxtHotkey.Text = text;
        _suppress = false;

        Cfg.HotkeyText = text;
        Cfg.SaveDebounced();
        MainWindow.Instance.RegisterGlobalHotkey();
        DashboardPage.RefreshPreviewIfVisible();
    }

    private async Task<bool> ConfirmSingleKeyHotkeyAsync(string text)
    {
        _confirmingSingleKey = true;
        try
        {
            var dialog = new ContentDialog
            {
                Title = Localizer.T(Localizer.S_ConfirmSingleKeyHotkeyTitle),
                Content = string.Format(Localizer.T(Localizer.S_ConfirmSingleKeyHotkeyMessage), text),
                PrimaryButtonText = Localizer.T(Localizer.S_SetAnyway),
                CloseButtonText = Localizer.T(Localizer.S_Cancel),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot,
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }
        finally
        {
            _confirmingSingleKey = false;
        }
    }

    private static bool IsFunctionKey(VirtualKey key) => key is >= VirtualKey.F1 and <= VirtualKey.F24;

    private static string GetHotkeyKeyName(VirtualKey key)
    {
        if (key is >= VirtualKey.A and <= VirtualKey.Z)
            return ((char)key).ToString();

        if (key is >= VirtualKey.Number0 and <= VirtualKey.Number9)
            return ((char)key).ToString();

        if (key is >= VirtualKey.F1 and <= VirtualKey.F24)
            return $"F{key - VirtualKey.F1 + 1}";

        var vk = (int)key;
        if (vk is >= 0x60 and <= 0x69)
            return $"Num {vk - 0x60}";

        return vk switch
        {
            0x08 => "Backspace",
            0x09 => "Tab",
            0x0D => "Enter",
            0x20 => "Space",
            0x1B => "Esc",
            0x2D => "Ins",
            0x2E => "Del",
            0x6A => "Num *",
            0x6B => "Num +",
            0x6D => "Num -",
            0x6E => "Num .",
            0x6F => "Num /",
            0xBA => ";",
            0xBB => "+",
            0xBC => ",",
            0xBD => "-",
            0xBE => ".",
            0xBF => "/",
            0xC0 => "`",
            0xDB => "[",
            0xDC => "\\",
            0xDD => "]",
            0xDE => "'",
            _ => key.ToString(),
        };
    }

    // ── Schedule ──

    private static void SetPickerTime(TimePicker picker, string timeStr)
    {
        if (TimeOnly.TryParse(timeStr, out var t))
            picker.Time = t.ToTimeSpan();
    }

    private void OnScheduleChanged(object _, RoutedEventArgs e)
    {
        if (_suppress) return;
        Cfg.ScheduledSwitch = ChkSchedule.IsOn;
        Cfg.Save();
        MainWindow.Instance.UpdateScheduleTimer();
        DashboardPage.RefreshPreviewIfVisible();
    }

    private void OnTimeOnChanged(object _, TimePickerValueChangedEventArgs e)
    {
        if (_suppress) return;
        ApplyDarkTime(e.NewTime.ToString(@"hh\:mm"));
    }

    private void OnTimeOffChanged(object _, TimePickerValueChangedEventArgs e)
    {
        if (_suppress) return;
        ApplyLightTime(e.NewTime.ToString(@"hh\:mm"));
    }

    private void ApplyDarkTime(string time)
    {
        if (!TimeOnly.TryParse(time, out _))
            return;

        Cfg.DarkTime = time;
        Cfg.Save();
        MainWindow.Instance.UpdateScheduleTimer();
        DashboardPage.RefreshPreviewIfVisible();
    }

    private void ApplyLightTime(string time)
    {
        if (!TimeOnly.TryParse(time, out _))
            return;

        Cfg.LightTime = time;
        Cfg.Save();
        MainWindow.Instance.UpdateScheduleTimer();
        DashboardPage.RefreshPreviewIfVisible();
    }

    // ── Toast Notifications ──

    private void OnToastNotificationChanged(object _, RoutedEventArgs e)
    {
        if (_suppress) return;
        Cfg.ToastNotification = ChkToastNotification.IsOn;
        Cfg.Save();
        DashboardPage.RefreshPreviewIfVisible();
    }

    private void OnToastSoundChanged(object _, RoutedEventArgs e)
    {
        if (_suppress) return;
        Cfg.ToastSound = ChkToastSound.IsOn;
        Cfg.Save();
        DashboardPage.RefreshPreviewIfVisible();
    }
}
