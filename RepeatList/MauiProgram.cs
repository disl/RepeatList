using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using RepeatList.Models;
using RepeatList.ViewModels;
using SQLitePCL;
#if ANDROID
using Microsoft.Maui;
#endif

namespace RepeatList
{
    using RepeatList.Controls;
#if ANDROID
    using RepeatList.Platforms.Android;
    using RepeatList.Platforms.Android.Services;
    using RepeatList.Services;
#endif

    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMediaElement()

    // Add this section anywhere on the builder:
    .UseSentry(options =>
    {
        // The DSN is the only required setting.
        options.Dsn = "https://b253a0732b2859186cc53692b1a9e625@o4509272206475264.ingest.de.sentry.io/4509272207982672";

        // Bekannter Google-Billing-Library-Bug (ProxyBillingActivity-NPE, Versionen 3.x–8.x):
        // wird von Play Pre-Launch-Reports / Bots ausgelöst, nicht vom App-Code
        // (es existiert kein aktiver Kauf-Flow). RevenueCat/Google empfehlen, diesen
        // Crash im Reporting zu unterdrücken statt zu fixen.
        // Hinweis: Sentry 6.8.0 nutzt die Methoden-API SetBeforeSend(...) statt der alten Property.
        options.SetBeforeSend((sentryEvent, hint) =>
        {
            // Pfad 1: Exception-Nachricht und/oder Stacktrace-Frames (deckt die innere NPE ab,
            // z. B. com.android.billingclient.api.ProxyBillingActivity.onCreate).
            bool isBillingProxyNpe =
                sentryEvent.SentryExceptions?.Any(ex =>
                    ex.Value?.Contains("ProxyBillingActivity") == true
                    || ex.Stacktrace?.Frames?.Any(f =>
                        f.Function?.Contains("ProxyBillingActivity") == true) == true) == true
                // Pfad 2: top-level Message (deckt die RuntimeException "Unable to start activity
                // ComponentInfo{.../ProxyBillingActivity}" ab; das native Event-Mapping überträgt
                // Exceptions evtl. nicht vollständig).
                || sentryEvent.Message?.Formatted?.Contains("ProxyBillingActivity") == true;

            return isBillingProxyNpe ? null : sentryEvent;
        });

        // Debug mode only in DEBUG builds — in Release verdeckt der Logcat-Noise
        // sonst den nativen ANR-Dump.
#if DEBUG
        options.Debug = true;
#else
        options.Debug = false;
#endif

        // ANR-Erkennung explizit aktivieren (Default ist true, hier bewusst gesetzt).
        options.Native.AnrEnabled = true;
        options.Native.AnrTimeoutInterval = TimeSpan.FromSeconds(3);

        // WICHTIG: Ohne dies filtert SetBeforeSend NUR .NET-Events. Der ProxyBillingActivity-Crash
        // ist ein nativer Android-Java-Crash und würde sonst ungefiltert ins Dashboard gehen.
        options.Native.EnableBeforeSend = true;

        // Other Sentry options can be set here.
        //options.TracesSampleRate = 1.0; // Optional: Performance-Tracing aktivieren
        //options.StackTraceMode = StackTraceMode.Original;
    })


                .ConfigureMauiHandlers(handlers =>
                {

#if ANDROID
                    handlers.AddHandler(typeof(AdBannerView), typeof(AdBannerViewHandler));
                    handlers.AddHandler<CollectionView, Platforms.Android.Handlers.CustomCollectionViewHandler>();
#endif

                })





            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

            builder.Services.AddSingleton<ListsPageViewModel>();
            builder.Services.AddSingleton<ListsPage>();

            builder.Services.AddSingleton<PositionsPageViewModel>();
            builder.Services.AddSingleton<PositionsPage>();

            builder.Services.AddSingleton<SetupPageViewModel>();
            builder.Services.AddSingleton<SetupPage>();

            builder.Services.AddSingleton<HelpPageViewModel>();
            builder.Services.AddSingleton<HelpPage>();

            builder.Services.AddSingleton<Header>();



            // Export file service
            builder.Services.AddSingleton<IFileExportService, FileExportService>();

#if ANDROID
            builder.Services.AddSingleton<IRewardedAdService, RewardedAdService>();
            builder.Services.AddSingleton<IAppOpenAdService, AppOpenAdService>();
            builder.Services.AddSingleton<IInterstitialAdService, InterstitialAdService>();
#endif



            //builder.Services.AddSingleton<Header>();  // Falls global
            //builder.Services.AddTransient<Header>();


            //builder.UseAdMob();

            //builder.Services.AddSingleton<Position>();  // Falls global
            //builder.Services.AddTransient<Position>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            // **SQLite initialisieren**
            Batteries.Init();


            // Für die plattformspezifische Implementierung von ISpeechToText den MauiApp und IMauiContext registrieren
            builder.Services.AddSingleton<Microsoft.Maui.Hosting.MauiApp>(provider =>
                        {
                            return provider.GetRequiredService<MauiApp>();
                        });


            builder.Services.AddSingleton<Microsoft.Maui.IMauiContext>(provider =>
            {
                var mauiApp = provider.GetRequiredService<MauiApp>();
                return mauiApp.Services.GetRequiredService<Microsoft.Maui.IMauiContext>();
            });


//#if ANDROID
//            builder.Services.AddSingleton<ISpeechToText>(provider =>
//            {
//                var mauiContext = provider.GetRequiredService<IMauiContext>();
//                return new SpeechToTextImplementation(mauiContext);
//            });
//#else
//                        builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);
//#endif

            // Für die plattformspezifische Implementierung von ISpeechToText den MauiApp und IMauiContext registrieren -- ENDE




//#if ANDROID
//            builder.Services.AddSingleton<IAudioTranscriber, AndroidTranscriber>();
//#elif IOS || MACCATALYST
//        builder.Services.AddSingleton<IAudioTranscriber, iOSTranscriber>();
//#elif WINDOWS
//        builder.Services.AddSingleton<IAudioTranscriber, WindowsTranscriber>();
//#endif

            //builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<DeepSeekClient>();


            return builder.Build();
        }
        //        public static MauiApp CreateMauiApp()
        //        {
        //            var builder = MauiApp.CreateBuilder();
        //            builder.UseMauiApp<App>()// Initialize the .NET MAUI Community Toolkit by adding the below line of code
        //            .UseMauiCommunityToolkit()
        //            .ConfigureServices(services =>
        //            {
        //                services.AddSingleton<ISpeechToText>(SpeechToText.Default);
        //            })
        //            .ConfigureFonts(fonts =>
        //            {
        //                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        //                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        //            })
        //            .UseMauiCommunityToolkitMediaElement();
        //#if DEBUG
        //            builder.Logging.AddDebug();
        //#endif
        //            // **SQLite initialisieren**
        //            Batteries.Init();
        //            return builder.Build();
        //        }
    }
}