using RepeatList.ViewModels;
using System.Globalization;

namespace RepeatList;

public partial class SetupPage : ContentPage
{
    public SetupPageViewModel ViewModel { get; set; }

    bool _isStart = true;
    AppTheme _appTheme;
    string _oldThema;

    string _oldLanguage;
    string _currLanguage;

    public SetupPage()
    {
        InitializeComponent();

        ViewModel = BindingContext as  SetupPageViewModel;

        //_oldThema = ViewModel.SelectedItem.DefaultAppTheme;
        //_oldLanguage = ViewModel.SelectedItem.DefaultLanguage;
        //_currLanguage=_oldLanguage;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isStart = true;

        _oldThema = ViewModel.SelectedItem.DefaultAppTheme;
        _oldLanguage = ViewModel.SelectedItem.DefaultLanguage;
        //_currLanguage=_oldLanguage;

        _ = ViewModel.Load();
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
            //_appTheme = AppTheme.Dark;
            Application.Current.UserAppTheme =  AppTheme.Dark;
        else
            //_appTheme = AppTheme.Light;
            Application.Current.UserAppTheme =  AppTheme.Light;
    }
}