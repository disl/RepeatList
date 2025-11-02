using Android;
using Android.App;
using Android.Content.PM;
using Android.Gms.Ads;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace RepeatList
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        const int RequestRecordAudioId = 101;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Initialisiere Mobile Ads SDK
            MobileAds.Initialize(this);

            CheckAudioPermission();

            // Aktivieren von EdgeToEdge
            //Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?.SetDecorFitsSystemWindows(false);

            Platform.Init(this, savedInstanceState);
        }

        void CheckAudioPermission()
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.RecordAudio) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.RecordAudio }, RequestRecordAudioId);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RequestRecordAudioId)
            {
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    Android.Util.Log.Info("VoiceRecognition", "Record Audio Permission granted.");
                }
                else
                {
                    Android.Util.Log.Warn("VoiceRecognition", "Record Audio Permission denied.");
                }
            }
        }
    }



}
