using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using RepeatList.Models;
using RepeatList.ViewModels;
using SQLitePCL;

namespace RepeatList
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>().UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            ;

            // Services
            //builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);

            builder.Services.AddSingleton<ListsPage>();
            builder.Services.AddSingleton<ListsPageViewModel>();

            builder.Services.AddSingleton<PositionsPage>();
            builder.Services.AddSingleton<PositionsPageViewModel>();

            builder.Services.AddSingleton<InputTextWithMicrophone>();
            builder.Services.AddSingleton<InputTextWithMicrophoneViewModel>();

            builder.Services.AddSingleton<SetupPage>();
            builder.Services.AddSingleton<SetupPageViewModel>();

            builder.Services.AddSingleton<HelpPage>();
            builder.Services.AddSingleton<HelpPageViewModel>();

            builder.Services.AddSingleton<Header>();  // Falls global
            builder.Services.AddTransient<Header>();

            //builder.Services.AddSingleton<Position>();  // Falls global
            //builder.Services.AddTransient<Position>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            // **SQLite initialisieren**
            Batteries.Init();

            //#if ANDROID
            //        builder.Services.AddSingleton<ISpeechToText>(new Android.SpeechToTextImplementation());
            //#else
            //            builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);
            //#endif


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