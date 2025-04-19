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
            //Android.Util.Log.Debug("MAUI_DEBUG", "App started in Release mode");

            base.OnCreate(savedInstanceState);

            // Initialisiere Mobile Ads SDK
            MobileAds.Initialize(this);


            // Aktivieren von EdgeToEdge
            //Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?.SetDecorFitsSystemWindows(false);

            //SetContentView(Resource.Layout.activity_main); // Verknüpfung mit XML
            //MobileAds.Initialize(this, this);

            //// Banner-Werbung laden
            //AdView adView = FindViewById<AdView>(Resource.Id.adView);
            //AdRequest adRequest = new AdRequest.Builder().Build();
            //adView.LoadAd(adRequest);
        }

        //public void OnInitializationComplete(IInitializationStatus status)
        //{
        //    // Hier kannst du z.B. prüfen, ob die Initialisierung erfolgreich war
        //    Android.Util.Log.Debug("AdMob", "AdMob SDK erfolgreich initialisiert");
        //}

        //void Init(MauiAppCompatActivity activity, string appId, string license = null, string nativeAdsId = null, string openAdsId = null, bool enableOpenAds = false, bool tagForUnderAgeOfConsent = false, string testDeviceId = null, bool forceTesting = false, DebugGeography geography = DebugGeography.DEBUG_GEOGRAPHY_DISABLED, bool initialiseConsentAtStartup = true, bool debugMode = false);

    }



}
