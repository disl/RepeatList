using CommunityToolkit.Mvvm.ComponentModel;
using RepeatList.Services;
using System.Globalization;

namespace RepeatList.ViewModels
{

    public partial class ResourcesViewModel : ObservableObject
    {
        private DatabaseService _databaseService;
        private SetupPageViewModel? setupPageViewModel;

        // Shell flyout title for the SetupPage (AppShell.xaml: Title="{Binding Setup}").
        // Was removed during the 14.08.26 refactor -> the binding resolved to null and
        // the "Einstellungen" menu item was rendered without a title (invisible).
        public string Setup => Properties.Resources.setup;

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
