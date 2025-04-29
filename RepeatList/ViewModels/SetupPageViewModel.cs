using CommunityToolkit.Mvvm.ComponentModel;
using RepeatList.Models;
using RepeatList.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

namespace RepeatList.ViewModels
{
    public partial class SetupPageViewModel : ObservableObject   // INotifyPropertyChanged,
    {
        #region PROPERTIES

        private DatabaseService _databaseService;
        //public event PropertyChangedEventHandler PropertyChanged;

        [ObservableProperty] public bool isChecked_Dark;
        partial void OnIsChecked_DarkChanged(bool oldValue, bool newValue)
        {
            if (SelectedItem != null)
            {
                SelectedItem.DefaultAppTheme = newValue ? "Dark" : "Light";
            }
        }
        [ObservableProperty] public bool isChecked_Light;
        partial void OnIsChecked_LightChanged(bool oldValue, bool newValue)
        {
            if (SelectedItem != null)
            {
                SelectedItem.DefaultAppTheme = newValue ? "Light" : "Dark";
            }
        }

        [ObservableProperty] public ObservableCollection<Setup> list = new ObservableCollection<Setup>();
        [ObservableProperty] public Setup? selectedItem;

        [ObservableProperty] public string title_language = Properties.Resources.language;
        [ObservableProperty] public string title_thema = Properties.Resources.theme;
        [ObservableProperty] public string label_cancel = Properties.Resources.Cancel;

        [ObservableProperty] public ObservableCollection<LanguageItem>? languages;
        [ObservableProperty] public LanguageItem? selectedLanguage;
        partial void OnSelectedLanguageChanged(LanguageItem? oldValue, LanguageItem? newValue)
        {
            if (SelectedItem != null && newValue != null)
            {
                SelectedItem.DefaultLanguage = newValue.Code;
            }
        }

        #endregion

        public SetupPageViewModel()
        {
            _databaseService = new DatabaseService();
            _= Load();

            InitLanguagePicker();
        }

        private void InitLanguagePicker()
        {
            Languages = new ObservableCollection<LanguageItem>
            {
                new LanguageItem { Name = "English", Code = "en", Icon = "england_icon.png" },
                new LanguageItem { Name = "Deutsch", Code = "de", Icon = "germany_icon.png" },
                new LanguageItem { Name = "Español", Code = "es", Icon = "spain_icon.png" },
                new LanguageItem { Name = "Français", Code = "fr", Icon = "france_icon.png" },
                new LanguageItem { Name = "Italiano", Code = "it", Icon = "italy_icon.png" },
                new LanguageItem { Name = "Українська", Code = "uk", Icon = "ukraine_icon.png" },
                new LanguageItem { Name = "Русский", Code = "ru", Icon = "russia_icon.png" },
            };

            // SelectedLanguage = Languages[0];
        }

        public async Task Load()
        {
            var _setup_list = await _databaseService.GetSetupsAsync();
            if (_setup_list == null || _setup_list.Count == 0)
            {
                await Add(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, "Dark");
            }
            else
            {
                if (List == null)
                    List = new ObservableCollection<Setup>();
                List.Clear();
                List = new ObservableCollection<Setup>(_setup_list);


                if (List != null &&  List.Count > 0)
                {
                    SelectedItem = List.FirstOrDefault();

                    // Thema
                    if (SelectedItem != null && Application.Current != null)
                    {
                        if (SelectedItem.DefaultAppTheme == "Dark")
                            Application.Current.UserAppTheme = AppTheme.Dark;
                        else
                            Application.Current.UserAppTheme = AppTheme.Light;

                        IsChecked_Dark = Application.Current.UserAppTheme == AppTheme.Dark;
                        IsChecked_Light = Application.Current.UserAppTheme == AppTheme.Light;
                    }

                    CultureInfo ci = new CultureInfo("en");
                    if (SelectedItem != null)
                        ci = new CultureInfo(SelectedItem.DefaultLanguage);
                    Thread.CurrentThread.CurrentCulture = ci;
                    Thread.CurrentThread.CurrentUICulture = ci;

                    if (Languages != null)
                    {
                        SelectedLanguage = Languages.FirstOrDefault(x => x.Code != null && x.Code.Equals(ci.TwoLetterISOLanguageName, StringComparison.CurrentCultureIgnoreCase))  ;
                    }
                }
            }
        }

        public async Task<int> Add(string DefaultLanguage, string DefaultAppTheme)
        {
            var newItem = new Setup { DefaultLanguage = DefaultLanguage, DefaultAppTheme = DefaultAppTheme };
            var new_id = await _databaseService.AddSetupAsync(newItem);

            await Load();

            var selectedItem = await _databaseService.GetSetupAsync(new_id);
            SelectedItem = selectedItem;

            return new_id;
        }

        public async Task DeleteSetup(Models.Setup Setup)
        {
            if (Setup == null) return;

            SelectedItem = Setup;
            await _databaseService.DeleteSetupAsync(Setup.Id);
            await Load();
        }

        public async Task UpdateSetup()
        {
            if (SelectedItem == null) return;

            await _databaseService.UpdateSetupAsync(SelectedItem);
            await Load();
        }

    }
}
