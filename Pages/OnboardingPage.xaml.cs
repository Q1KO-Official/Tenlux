using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Tenlux.Helpers;
using static Tenlux.Helpers.Localizer;

namespace Tenlux.Pages;

public sealed partial class OnboardingPage : Page
{
    private int _step;
    private const int TotalSteps = 6; // welcome + 4 features + ready
    private readonly List<Ellipse> _dots = [];

    public OnboardingPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private bool _suppress;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.SetTitleBar(null);
        InitLangCombo();
        ApplyLabels();
        CreateDots();
        UpdateUI();
        AnimateStepIn();
    }

    private void InitLangCombo()
    {
        _suppress = true;
        CmbLang.Items.Clear();
        PopulateLangCombo(CmbLang);
        _suppress = false;
    }

    private void ApplyLabels()
    {
        TxtWelcomeAppName.Text = T(S_AppName);
        WelcomeDesc.Text = T(S_OnWelcomeDesc);
        WelcomeHint.Text = T(S_OnboardingHint);
        BtnWelcomeNext.Content = T(S_Next);

        Feature1Title.Text = T(S_OnThemeTitle);
        Feature1Desc.Text = T(S_OnThemeDesc);
        Feature2Title.Text = T(S_OnWallpaperTitle);
        Feature2Desc.Text = T(S_OnWallpaperDesc);
        Feature3Title.Text = T(S_OnHotkeyTitle);
        Feature3Desc.Text = T(S_OnHotkeyDesc);
        Feature4Title.Text = T(S_OnStartTitle);
        Feature4Desc.Text = T(S_OnStartDesc);
        ReadyTitle.Text = T(S_OnReadyTitle);
        ReadyDesc.Text = T(S_OnReadyDesc);

        BtnSkip.Content = T(S_Skip);
        BtnPrev.Content = T(S_Previous);
    }

    private void CreateDots()
    {
        DotsPanel.Children.Clear();
        _dots.Clear();
        for (int i = 0; i < TotalSteps; i++)
        {
            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = (Brush)Application.Current.Resources["ControlStrongFillColorDisabledBrush"]
            };
            DotsPanel.Children.Add(dot);
            _dots.Add(dot);
        }
    }

    private void UpdateUI()
    {
        WelcomePanel.Visibility = _step == 0 ? Visibility.Visible : Visibility.Collapsed;
        FeaturePanel.Visibility = _step is >= 1 and <= 4 ? Visibility.Visible : Visibility.Collapsed;

        Feature1.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed;
        Feature2.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed;
        Feature3.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed;
        Feature4.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed;
        ReadyPanel.Visibility = _step == 5 ? Visibility.Visible : Visibility.Collapsed;

        var accentBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var dimBrush = (Brush)Application.Current.Resources["ControlStrongFillColorDisabledBrush"];
        for (int i = 0; i < _dots.Count; i++)
            _dots[i].Fill = i == _step ? accentBrush : dimBrush;

        if (_step == 0)
        {
            BtnSkip.Visibility = Visibility.Visible;
            BtnPrev.Visibility = Visibility.Collapsed;
            BtnNext.Visibility = Visibility.Collapsed;
        }
        else if (_step == TotalSteps - 1)
        {
            BtnSkip.Visibility = Visibility.Collapsed;
            BtnPrev.Visibility = Visibility.Visible;
            BtnNext.Visibility = Visibility.Visible;
            BtnNext.Content = T(S_StartUsing);
        }
        else
        {
            BtnSkip.Visibility = Visibility.Visible;
            BtnPrev.Visibility = Visibility.Visible;
            BtnNext.Visibility = Visibility.Visible;
            BtnNext.Content = T(S_Next);
        }
    }

    private void OnLangChanged(object _, SelectionChangedEventArgs e)
    {
        if (_suppress || CmbLang.SelectedIndex < 0 || CmbLang.SelectedIndex == Lang) return;
        Lang = CmbLang.SelectedIndex;
        App.Settings.Save();
        RefreshLangCombo();
        ApplyLabels();
    }

    private void RefreshLangCombo()
    {
        _suppress = true;
        Localizer.RefreshLangCombo(CmbLang);
        _suppress = false;
    }

    private UIElement GetStepPanel() => _step switch
    {
        0 => WelcomePanel,
        5 => ReadyPanel,
        _ => FeaturePanel,
    };

    private void AnimateStepIn()
    {
        var panel = GetStepPanel();
        panel.Opacity = 0;
        var anim = new DoubleAnimation
        {
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(250)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(anim, panel);
        Storyboard.SetTargetProperty(anim, "Opacity");
        var sb = new Storyboard();
        sb.Children.Add(anim);
        sb.Begin();
    }

    private void OnNext(object sender, RoutedEventArgs e)
    {
        if (_step < TotalSteps - 1)
        {
            _step++;
            UpdateUI();
            AnimateStepIn();
        }
        else
        {
            FinishOnboarding();
        }
    }

    private void OnSkip(object sender, RoutedEventArgs e)
    {
        FinishOnboarding();
    }

    private void OnPrev(object sender, RoutedEventArgs e)
    {
        if (_step > 0)
        {
            _step--;
            UpdateUI();
            AnimateStepIn();
        }
    }

    private async void FinishOnboarding()
    {
        App.Settings.FirstRunDone = true;
        App.Settings.Save();
        Frame.Navigate(typeof(SettingsPage));
        Frame.BackStack.Clear();

        // Wait for SettingsPage XamlRoot to be ready
        for (int i = 0; i < 15; i++)
        {
            if (Frame.XamlRoot != null) break;
            await Task.Delay(200);
        }

        MainWindow.Instance.ShowTrayTutorial();
    }
}
