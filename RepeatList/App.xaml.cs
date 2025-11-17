using RepeatList.Pages;

namespace RepeatList
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                // Fuer nicht beobachtete Task-Ausnahmen
                e.SetObserved();
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Task-Fehler", e.Exception.Message, "OK");

                    SentrySdk.CaptureException(e.Exception);
                });
            };

            // Datenbankdatei kopieren
            Task.Run(async () => await DatabaseHelper.CopyDatabaseToAppData("todo.db3")).Wait();

          
        }

        private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;

            if (ex != null)
                SentrySdk.CaptureException(ex);

            // Zurück zum MainThread wechseln
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current.MainPage.DisplayAlert("Untreated exception. Please report the exception to support.", ex?.Message ?? "Unknown error. Please report the error to support.", "OK");
            });

            // Optional: Logging oder Fehlerberichterstattung
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            bool seen = Preferences.Get("onboarding_seen", false);

            if (!seen)
                return new Window(new OnboardingPage());
            else
                return new Window(new AppShell());
        }


    }
}