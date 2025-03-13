using RepeatList.ViewModels;

namespace RepeatList
{
    public partial class AppShell : Shell
    {
        private ResourcesViewModel ViewModel { get; set; }

        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("Lists", typeof(ListsPage));
            Routing.RegisterRoute("Help", typeof(HelpPage));
            Routing.RegisterRoute("Positions", typeof(PositionsPage));
            Routing.RegisterRoute("Lists/Positions", typeof(PositionsPage));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ViewModel = new ResourcesViewModel();
            BindingContext = ViewModel;
        }


    }
}
