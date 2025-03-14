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
        public event PropertyChangedEventHandler PropertyChanged;

        [ObservableProperty] public ObservableCollection<Setup> list = new ObservableCollection<Setup>();
        [ObservableProperty] public Setup? selectedItem;

        [ObservableProperty] public string title_language = Properties.Resources.language;
        [ObservableProperty] public string title_thema = Properties.Resources.theme;
        [ObservableProperty] public string label_cancel = Properties.Resources.Cancel;

        [ObservableProperty] public ObservableCollection<LanguageItem> languages;
        [ObservableProperty] public LanguageItem selectedLanguage;
        partial void OnSelectedLanguageChanged(LanguageItem oldValue, LanguageItem newValue)
        {
            SelectedItem.DefaultLanguage = newValue.Code;
        }


        #endregion

        //public double ButtonsSize = 30;

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
                new LanguageItem { Name = "Українська", Code = "ua", Icon = "ukraine_icon.png" },
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
                    if (SelectedItem.DefaultAppTheme == "Dark")
                        Application.Current.UserAppTheme = AppTheme.Dark;
                    else
                        Application.Current.UserAppTheme = AppTheme.Light;

                    // Language
                    CultureInfo ci = new CultureInfo("en");
                    ci = new CultureInfo(SelectedItem.DefaultLanguage);

                    Thread.CurrentThread.CurrentCulture = ci;
                    Thread.CurrentThread.CurrentUICulture = ci;

                    SelectedLanguage = Languages.FirstOrDefault(x => x.Code.ToLower() == ci.TwoLetterISOLanguageName.ToLower());
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
