using CommunityToolkit.Mvvm.ComponentModel;
using RepeatList.Services;
using System.Globalization;

namespace RepeatList.ViewModels
{

    public partial class ResourcesViewModel : ObservableObject
    {
        private DatabaseService _databaseService;
        private SetupPageViewModel? setupPageViewModel;

        [ObservableProperty] public static string setup;

        public ResourcesViewModel()
        {
            _databaseService = new DatabaseService();

            setupPageViewModel = new SetupPageViewModel();
            _ = setupPageViewModel.Load();
            var CurrentCulture = setupPageViewModel.SelectedItem.DefaultLanguage;
            CultureInfo culture = new CultureInfo(CurrentCulture);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            Setup = Properties.Resources.setup;
        }


    }
}
