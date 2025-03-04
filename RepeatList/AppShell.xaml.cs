using System.Globalization;

namespace RepeatList
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("MainPage", typeof(Lists));
        }
    }
}
