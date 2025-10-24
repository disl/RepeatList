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


        // Use debug mode if you want to see what the SDK is doing.
        // Debug messages are written to stdout with Console.Writeline,
        // and are viewable in your IDE's debug console or with 'adb logcat', etc.
        // This option is not recommended when deploying your application.
        options.Debug = true;

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


#if ANDROID
            builder.Services.AddSingleton<ISpeechToText>(provider =>
            {
                var mauiContext = provider.GetRequiredService<IMauiContext>();
                return new SpeechToTextImplementation(mauiContext);
            });
#else
                        builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);
#endif

            // Für die plattformspezifische Implementierung von ISpeechToText den MauiApp und IMauiContext registrieren -- ENDE




#if ANDROID
            builder.Services.AddSingleton<IAudioTranscriber, AndroidTranscriber>();
#elif IOS || MACCATALYST
        builder.Services.AddSingleton<IAudioTranscriber, iOSTranscriber>();
#elif WINDOWS
        builder.Services.AddSingleton<IAudioTranscriber, WindowsTranscriber>();
#endif

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