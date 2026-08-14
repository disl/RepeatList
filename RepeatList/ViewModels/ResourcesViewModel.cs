using CommunityToolkit.Mvvm.ComponentModel;
using RepeatList.Services;
using System.Globalization;

namespace RepeatList.ViewModels
{

    public partial class ResourcesViewModel : ObservableObject
    {
        private DatabaseService _databaseService;
        private SetupPageViewModel? setupPageViewModel;

        public ResourcesViewModel()
        {
            _databaseService = new DatabaseService();

            setupPageViewModel = new SetupPageViewModel();
            // Kein synchroner Zugriff auf SelectedItem: Load() läuft dank echtem Async
            // nicht mehr synchron durch → SelectedItem ist sonst null (NullReferenceException).
            _ = InitializeCultureAsync();
        }

        private async Task InitializeCultureAsync()
        {
            try
            {
                await setupPageViewModel.Load();

                var lang = setupPageViewModel.SelectedItem?.DefaultLanguage;
                if (!string.IsNullOrEmpty(lang))
                {
                    var culture = new CultureInfo(lang);
                    CultureInfo.DefaultThreadCurrentCulture = culture;
                    CultureInfo.DefaultThreadCurrentUICulture = culture;
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
        }


    }
}
