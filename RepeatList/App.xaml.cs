namespace RepeatList
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Load the default theme
            //LoadTheme("DarkTheme.xaml");

            // Datenbankdatei kopieren
            Task.Run(async () => await DatabaseHelper.CopyDatabaseToAppData("todo.db3")).Wait();

            //MainPage = new MainPage();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        public void LoadTheme(string themeName)
        {
            //try
            //{
            //    // Clear existing resources
            //    Resources.MergedDictionaries.Clear();

            //    // Load the new theme
            //    var assembly = GetType().Assembly;
            //    var themePath = $"RepeatList.Themes.{themeName}";
            //    using (var stream = assembly.GetManifestResourceStream(themePath))
            //    {
            //        using (StreamReader reader = new StreamReader(stream))
            //        {
            //            var theme = reader.ReadToEnd();
            //            //var res_dict = theme as ResourceDictionary;
            //            if (theme != null)
            //            {
            //                Resources.MergedDictionaries.Add(theme);
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex) { }
        }
    }
}