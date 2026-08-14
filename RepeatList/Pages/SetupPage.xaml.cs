using RepeatList.Services;
using RepeatList.ViewModels;
using System.Globalization;

namespace RepeatList;

public partial class SetupPage : ContentPage
{
    public SetupPageViewModel ViewModel { get; set; }=new SetupPageViewModel();

    bool _isStart = true;
    AppTheme _appTheme;
    string _oldThema;

    string _oldLanguage;
    string _currLanguage;
    bool _aiSaving;
    string? _aiKeysUrl;

    public SetupPage()
    {
        InitializeComponent();

        ViewModel = new SetupPageViewModel();
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isStart = true;

        // SelectedItem ist erst nach Load() verfügbar (davor null → NullReferenceException).
        await ViewModel.Load();

        _oldThema = ViewModel.SelectedItem?.DefaultAppTheme ?? "Dark";
        _oldLanguage = ViewModel.SelectedItem?.DefaultLanguage ?? "en";

        _ = LoadAiSettingsAsync();
    }

    // ── AI provider settings ──
    private async Task LoadAiSettingsAsync()
    {
        AiSectionTitle.Text = AiSettingsService.T("AiSettingsSection");
        AiProviderLabel.Text = AiSettingsService.T("SettingsProviderLabel");
        AiApiKeyLabel.Text = AiSettingsService.T("SettingsApiKeyLabel");
        AiBaseUrlLabel.Text = AiSettingsService.T("SettingsBaseUrlLabel");
        AiModelLabel.Text = AiSettingsService.T("SettingsModelLabel");
        AiSaveBtn.Text = AiSettingsService.T("SettingsSaveAndTest");

        AiProviderPicker.ItemsSource ??= new[]
        {
            AiSettingsService.T("ProviderDeepSeek"),
            AiSettingsService.T("ProviderOpenRouter"),
            AiSettingsService.T("ProviderCustom")
        };

        var settings = await AiSettingsService.Instance.LoadAsync();
        AiProviderPicker.SelectedIndex = ProviderToIndex(settings.Provider);
        AiApiKeyEntry.Text = settings.ApiKey;
        AiBaseUrlEntry.Text = settings.BaseUrl;
        AiModelEntry.Text = settings.Model;

        OnAiProviderChanged(null, EventArgs.Empty);
    }

    private void OnAiProviderChanged(object? sender, EventArgs e)
    {
        (string linkKey, string? baseUrl, string? model, string keysUrl) = AiProviderPicker.SelectedIndex switch
        {
            1 => ("ApiKeyLinkOpenRouter", AiSettingsService.OpenRouterBaseUrl, AiSettingsService.OpenRouterDefaultModel, AiSettingsService.OpenRouterKeysUrl),
            2 => ("ApiKeyLinkCustom", null, null, AiSettingsService.CustomKeysUrl),
            _ => ("ApiKeyLinkDeepSeek", AiSettingsService.DeepSeekBaseUrl, AiSettingsService.DeepSeekModel, AiSettingsService.DeepSeekKeysUrl)
        };

        AiApiKeyLinkLabel.Text = AiSettingsService.T(linkKey);
        _aiKeysUrl = keysUrl;

        if (baseUrl is not null) AiBaseUrlEntry.Text = baseUrl;
        if (model is not null) AiModelEntry.Text = model;
    }

    private async void OnAiApiKeyLinkTapped(object? sender, TappedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_aiKeysUrl)) return;

        try
        {
            await Browser.OpenAsync(_aiKeysUrl);
        }
        catch (Exception ex)
        {
            AiStatusLabel.Text = ex.Message;
            AiStatusLabel.IsVisible = true;
        }
    }

    private async void OnAiSaveClicked(object? sender, EventArgs e)
    {
        if (_aiSaving) return;

        var baseUrl = AiBaseUrlEntry.Text?.Trim() ?? "";
        var model = AiModelEntry.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(model))
        {
            ShowAiStatus(AiSettingsService.T("SettingsInvalid"), isError: true);
            return;
        }

        var settings = new AiSettings(
            IndexToProvider(AiProviderPicker.SelectedIndex),
            AiApiKeyEntry.Text?.Trim() ?? "",
            baseUrl,
            model);

        _aiSaving = true;
        AiSaveBtn.IsEnabled = false;
        AiBusyIndicator.IsRunning = true;
        ShowAiStatus(AiSettingsService.T("SettingsTesting"), isError: false);

        try
        {
            // Test first; only persist once the endpoint/model is confirmed reachable.
            await new DeepSeekClient().TestConnectionAsync(settings);
            await AiSettingsService.Instance.SaveAsync(settings);
            ShowAiStatus(AiSettingsService.T("SettingsTestOk"), isError: false);
        }
        catch (Exception ex)
        {
            ShowAiStatus(string.Format(AiSettingsService.T("SettingsTestFailed"), ex.Message), isError: true);
        }
        finally
        {
            _aiSaving = false;
            AiSaveBtn.IsEnabled = true;
            AiBusyIndicator.IsRunning = false;
        }
    }

    private static int ProviderToIndex(string provider) => provider switch
    {
        AiSettingsService.ProviderOpenRouter => 1,
        AiSettingsService.ProviderCustom => 2,
        _ => 0
    };

    private static string IndexToProvider(int index) => index switch
    {
        1 => AiSettingsService.ProviderOpenRouter,
        2 => AiSettingsService.ProviderCustom,
        _ => AiSettingsService.ProviderDeepSeek
    };

    private void ShowAiStatus(string message, bool isError)
    {
        AiStatusLabel.Text = message;

        // Readable in both themes: bright shades on dark, standard colors on light.
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        AiStatusLabel.TextColor = isError
            ? (isDark ? Color.FromArgb("#FF8A80") : Colors.Red)
            : (isDark ? Color.FromArgb("#69F0AE") : Colors.Green);

        AiStatusLabel.IsVisible = true;
    }

    private async void OkButton_Clicked(object sender, EventArgs e)
    {
        // Save in DB
        await ViewModel.UpdateSetup();

        //Application.Current.UserAppTheme =  _appTheme;
        if (_currLanguage != null)
            SetCurrentCulture(_currLanguage);

        if (_oldLanguage != ViewModel.SelectedItem.DefaultLanguage || _oldThema != ViewModel.SelectedItem.DefaultAppTheme)
        {
            // 
            await DisplayAlert("Information", Properties.Resources.Application_is_closed_to_update_changes, "OK");
            Application.Current?.Quit();
        }
        else
            await Shell.Current.GoToAsync("//Lists");
    }

    private void SetCurrentCulture(string curr_culture)
    {
        try
        {
            //CultureInfo ci = new CultureInfo("en");
            var ci = new CultureInfo(curr_culture);
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }
        catch(Exception ex)
        {
            if (ex != null)
                SentrySdk.CaptureException(ex);
        }
    }

    private async void OnCancel(object sender, EventArgs e)
    {
        // Thema
        if (_oldThema == "Dark")
            Application.Current.UserAppTheme = AppTheme.Dark;
        else
            Application.Current.UserAppTheme = AppTheme.Light;

        // Language
        SetCurrentCulture(_oldLanguage);

        await Shell.Current.GoToAsync("//Lists");
    }

    private void LanguagePicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_isStart)
        {
            _isStart=false;
            return;
        }
        if (ViewModel == null)
            return;
        _currLanguage = ViewModel.SelectedItem.DefaultLanguage;
    }

    private async void ThemaPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_isStart)
        {
            _isStart=false;
            return;
        }
        if (ViewModel == null)
            return;
        if (ViewModel.SelectedItem.DefaultAppTheme == "Dark")
            Application.Current.UserAppTheme =  AppTheme.Dark;
        else
            Application.Current.UserAppTheme =  AppTheme.Light;
    }

    private void ForRadioButtonCheckedChanged()
    {
        if (_isStart)
        {
            _isStart=false;
            return;
        }
        if (ViewModel == null)
            return;
        if (ViewModel.SelectedItem.DefaultAppTheme == "Dark")
            Application.Current.UserAppTheme =  AppTheme.Dark;
        else
            Application.Current.UserAppTheme =  AppTheme.Light;
    }

    private void RadioButtonCheckedChanged_dark(object sender, CheckedChangedEventArgs e)
    {
        //ViewModel.SelectedItem.DefaultAppTheme = "Dark";
        //ForRadioButtonCheckedChanged();
    }

    private void RadioButtonCheckedChanged_light(object sender, CheckedChangedEventArgs e)
    {
        //ViewModel.SelectedItem.DefaultAppTheme = "Light";
        //ForRadioButtonCheckedChanged();
    }

    private void SortingPicker_SelectedIndexChanged(object sender, EventArgs e)
    {

    }
}