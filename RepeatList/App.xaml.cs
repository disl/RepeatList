namespace RepeatList
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Datenbankdatei kopieren
            Task.Run(async () => await DatabaseHelper.CopyDatabaseToAppData("todo.db3")).Wait();

            //MainPage = new MainPage();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}