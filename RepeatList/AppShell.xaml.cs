using RepeatList.ViewModels;

namespace RepeatList
{
    public partial class AppShell : Shell
    {
        private ResourcesViewModel ViewModel { get; set; }

        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("Help", typeof(HelpPage));
            Routing.RegisterRoute("Lists/Positions", typeof(PositionsPage));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ViewModel = new ResourcesViewModel();
            BindingContext = ViewModel;
        }

        private async void OnRateAppClicked(object sender, EventArgs e)
        {
            string url = "";

#if ANDROID
            url = $"https://play.google.com/store/apps/details?id={AppInfo.PackageName}";
            //#elif IOS
            //            url = "https://apps.apple.com/app/idDEINE_APP_ID";
            //#else
            //            url = "https://google.com";
#endif
            await Launcher.Default.OpenAsync(url);
        }

    }
}
