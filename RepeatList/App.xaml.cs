using RepeatList.Pages;
using RepeatList.Services;
using Microsoft.Extensions.DependencyInjection;

namespace RepeatList
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Nur Ereignis-Handler im Konstruktor registrieren
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                e.SetObserved();
                SentrySdk.CaptureException(e.Exception);
            };

            // WICHTIG: Setze eine leere Platzhalterseite, 
            // damit die App sofort etwas zum Anzeigen hat.
            MainPage = new ContentPage { BackgroundColor = Colors.White };
        }

        protected override async void OnStart()
        {
            base.OnStart();

            try
            {
                // Datenbank asynchron ohne Blockieren kopieren
                await DatabaseHelper.CopyDatabaseToAppData("todo.db3");

                // Abo-Status von Supabase laden (im Hintergrund, kein Blockieren)
                _ = Task.Run(async () =>
                {
                    try { await new InAppBillingService().SyncFromSupabaseAsync(); }
                    catch (Exception) { }
                });

                // Erst nach dem Kopieren die richtige Seite setzen
                bool seen = Preferences.Get("onboarding_seen", false);

                if (!seen)
                {
                    MainPage = new OnboardingPage();
                }
                else
                {
                    MainPage = new AppShell();

                    // Pre-load interstitial ad early so it's ready when a list is completed
                    _ = IPlatformApplication.Current?.Services.GetService<IInterstitialAdService>();

                    // App Open Ad zeigen — bis zu 5 Sek. auf Laden warten
                    _ = Task.Run(async () =>
                    {
                        var adService = IPlatformApplication.Current?.Services.GetService<IAppOpenAdService>();
                        if (adService == null) return;

                        await Task.Delay(800); // Activity muss bereit sein
                        for (int i = 0; i < 10; i++)
                        {
                            if (adService.IsAdReady)
                            {
                                await MainThread.InvokeOnMainThreadAsync(() => adService.ShowAdIfReadyAsync());
                                break;
                            }
                            await Task.Delay(500);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                // Fallback bei Fehlern
                MainPage = new AppShell();
            }
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            AppLifecycle.NotifyBackgrounded();
        }

        protected override void OnResume()
        {
            base.OnResume();
            AppLifecycle.NotifyForegrounded();
        }

        private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                SentrySdk.CaptureException(ex);
        }
    }



    //public partial class App : Application
    //{
    //    public App()
    //    {
    //        InitializeComponent();

    //        // 1. Fehlerbehandlung (Sentry & Co)
    //        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
    //        TaskScheduler.UnobservedTaskException += (s, e) =>
    //        {
    //            e.SetObserved();
    //            MainThread.BeginInvokeOnMainThread(async () =>
    //            {
    //                if (Application.Current?.MainPage != null)
    //                    await Application.Current.MainPage.DisplayAlert("Task-Fehler", e.Exception.Message, "OK");
    //                SentrySdk.CaptureException(e.Exception);
    //            });
    //        };

    //        // 2. Datenbank-Initialisierung
    //        // Hinweis: .Wait() kann Deadlocks verursachen. GetAwaiter().GetResult() ist in Konstruktoren oft stabiler.
    //        DatabaseHelper.CopyDatabaseToAppData("todo.db3").GetAwaiter().GetResult();

    //        // 3. NAVIGATIONSLOGIK (Die Lösung des Fehlers)
    //        bool seen = Preferences.Get("onboarding_seen", false);

    //        if (!seen)
    //        {
    //            // Wenn Onboarding noch nicht gesehen, zeige OnboardingPage
    //            MainPage = new OnboardingPage();
    //        }
    //        else
    //        {
    //            // Ansonsten starte die normale AppShell
    //            MainPage = new AppShell();
    //        }
    //    }

    //    private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
    //    {
    //        Exception ex = e.ExceptionObject as Exception;
    //        if (ex != null) SentrySdk.CaptureException(ex);

    //        MainThread.BeginInvokeOnMainThread(async () =>
    //        {
    //            if (Application.Current?.MainPage != null)
    //                await Application.Current.MainPage.DisplayAlert("Unbehandelter Fehler", ex?.Message ?? "Unbekannter Fehler", "OK");
    //        });
    //    }

    //    // LÖSCHEN SIE DIE KOMPLETTE METHODE "CreateWindow" UNTEN IN IHREM CODE!
    //    // protected override Window CreateWindow(IActivationState? activationState) { ... }
    //}
    //}






    //using RepeatList.Pages;

    //namespace RepeatList
    //{
    //    public partial class App : Application
    //    {
    //        public App()
    //        {
    //            InitializeComponent();

    //            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

    //            TaskScheduler.UnobservedTaskException += (s, e) =>
    //            {
    //                // Fuer nicht beobachtete Task-Ausnahmen
    //                e.SetObserved();
    //                MainThread.BeginInvokeOnMainThread(async () =>
    //                {
    //                    //if (Application.Current != null && Application.Current?.MainPage != null)
    //                    if (Application.Current?.MainPage != null)
    //                        await Application.Current.MainPage.DisplayAlert("Task-Fehler", e.Exception.Message, "OK");

    //                    SentrySdk.CaptureException(e.Exception);
    //                });
    //            };

    //            // Datenbankdatei kopieren
    //            Task.Run(async () => await DatabaseHelper.CopyDatabaseToAppData("todo.db3")).Wait();


    //        }

    //        private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
    //        {
    //            Exception ex = e.ExceptionObject as Exception;

    //            if (ex != null)
    //                SentrySdk.CaptureException(ex);

    //            // Zurück zum MainThread wechseln
    //            MainThread.BeginInvokeOnMainThread(async () =>
    //            {
    //                if (Application.Current != null && Application.Current?.MainPage != null)
    //                    await Application.Current.MainPage.DisplayAlert("Untreated exception. Please report the exception to support.",
    //                        ex?.Message ?? "Unknown error. Please report the error to support.", "OK");
    //            });

    //            // Optional: Logging oder Fehlerberichterstattung
    //        }

    //        protected override Window CreateWindow(IActivationState? activationState)
    //        {
    //            bool seen = Preferences.Get("onboarding_seen", false);


    //            // TEST
    //            //seen = false;

    //            if (!seen)
    //            {
    //                try
    //                {
    //                    return new Window(new OnboardingPage());
    //                }
    //                catch (Exception ex)
    //                {
    //                    SentrySdk.CaptureException(ex);
    //                    return new Window(new AppShell());
    //                }
    //            }
    //            else
    //                return new Window(new AppShell());

    //        }


    //    }
}