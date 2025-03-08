namespace RepeatList
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("Lists", typeof(ListsPage));
            Routing.RegisterRoute("Help", typeof(HelpPage));
            Routing.RegisterRoute("Positions", typeof(PositionsPage));
            Routing.RegisterRoute("Lists/Positions", typeof(PositionsPage));
        }
    }
}
