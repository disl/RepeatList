using System.Globalization;

namespace RepeatList
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("Positions", typeof(PositionsPage));
            Routing.RegisterRoute("Lists", typeof(ListsPage));
        }
    }
}
