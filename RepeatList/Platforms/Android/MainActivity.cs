using Android.App;
using Android.Content.PM;
using Android.Gms.Ads;
using Android.OS;

namespace RepeatList
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity  //, IOnInitializationCompleteListener
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Initialisiere Mobile Ads SDK
            MobileAds.Initialize(this);


            // Aktivieren von EdgeToEdge
            //Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?.SetDecorFitsSystemWindows(false);
        }

       
    }



}
