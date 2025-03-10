using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Gms.Ads;

namespace RepeatList
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            //// Banner-Werbung laden

            SetContentView(Resource.Layout.activity_main); // Verknüpfung mit XML

            MobileAds.Initialize(this);
            //MobileAds.Initialize(this, "ca-app-pub-3940256099942544~3347510173"); // Deine App-ID

            // Banner-Werbung laden
            AdView adView = FindViewById<AdView>(Resource.Id.adView);
            AdRequest adRequest = new AdRequest.Builder().Build();
            adView.LoadAd(adRequest);
        }

    }



}
