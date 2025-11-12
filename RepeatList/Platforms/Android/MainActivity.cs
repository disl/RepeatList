using Android;
using Android.App;
using Android.Content.PM;
using Android.Gms.Ads;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Bumptech.Glide.Load.Model;
using RepeatList.ViewModels;
using System.Collections.ObjectModel;

namespace RepeatList
{
    //[Activity(
    //    Theme = "@style/Maui.SplashTheme", 
    //    MainLauncher = true, 
    //    LaunchMode = LaunchMode.SingleTop, 
    //    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)
    //]

    [Activity(
        Exported = true,
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)
    ]
    [IntentFilter(new[] { Android.Content.Intent.ActionView },
              Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },

              DataMimeType = "application/json",
              DataSchemes = new[] { "content", "file" })]
    public class MainActivity : MauiAppCompatActivity
    {
        const int RequestRecordAudioId = 101;

        private static string? _pendingIntentJson { get; set; }

        private readonly SemaphoreSlim _sync = new(0, 1);

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Initialisiere Mobile Ads SDK
            MobileAds.Initialize(this);

            CheckAudioPermission();

            // Aktivieren von EdgeToEdge
            //Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?.SetDecorFitsSystemWindows(false);

            Platform.Init(this, savedInstanceState);

            // JSON-file Serializer Optionen setzen

            _pendingIntentJson = string.Empty;

            try
            {
                if (Intent?.Data != null)
                {
                    var uri = Intent.Data;

                    using var stream = ContentResolver.OpenInputStream(uri);
                    using var reader = new StreamReader(stream);
                    string json = reader.ReadToEnd();
                    _pendingIntentJson = json;
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("MainActivity", $"Error processing intent data: {ex.Message}");
            }
        }



        public static string GetPendingIntentData()
        {
            var data = _pendingIntentJson;
            _pendingIntentJson = null; // Zurücksetzen nach dem Abholen
            return data;
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
