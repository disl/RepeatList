using Android.Gms.Ads;
using RepeatList.Services;

using GmsInterstitialAd = Android.Gms.Ads.Interstitial.InterstitialAd;
using GmsInterstitialLoadCallback = Android.Gms.Ads.Interstitial.InterstitialAdLoadCallback;
using GmsLoadError = Android.Gms.Ads.LoadAdError;
using GmsAdError = Android.Gms.Ads.AdError;

namespace RepeatList.Platforms.Android.Services
{
    public class InterstitialAdService : IInterstitialAdService
    {
        private const string AdUnitId = "ca-app-pub-9459821903521146/3552489938";

        private GmsInterstitialAd? _interstitialAd;
        private bool _isLoading;

        public bool IsAdReady => _interstitialAd != null;

        public InterstitialAdService()
        {
            LoadAd();
        }

        public void LoadAd()
        {
            if (_isLoading || IsAdReady) return;
            _isLoading = true;

            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity == null) { _isLoading = false; return; }

            activity.RunOnUiThread(() =>
            {
                var request = new AdRequest.Builder().Build();
                GmsInterstitialAd.Load(activity, AdUnitId, request, new InterstitialLoadCallback(
                    onLoaded: ad =>
                    {
                        _interstitialAd = ad;
                        _isLoading = false;
                    },
                    onFailed: _ =>
                    {
                        _interstitialAd = null;
                        _isLoading = false;
                    }
                ));
            });
        }

        public Task ShowAdIfReadyAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            if (!IsAdReady)
            {
                tcs.SetResult(false);
                return tcs.Task;
            }

            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity == null) { tcs.SetResult(false); return tcs.Task; }

            var ad = _interstitialAd;
            _interstitialAd = null;

            activity.RunOnUiThread(() =>
            {
                ad!.FullScreenContentCallback = new InterstitialContentCallback(
                    onDismissed: () =>
                    {
                        LoadAd();
                        tcs.TrySetResult(true);
                    },
                    onFailed: _ =>
                    {
                        LoadAd();
                        tcs.TrySetResult(false);
                    }
                );
                ad.Show(activity);
            });

            return tcs.Task;
        }
    }

    internal sealed class InterstitialLoadCallback : GmsInterstitialLoadCallback
    {
        private readonly Action<GmsInterstitialAd> _onLoaded;
        private readonly Action<GmsLoadError> _onFailed;

        public InterstitialLoadCallback(Action<GmsInterstitialAd> onLoaded, Action<GmsLoadError> onFailed)
        {
            _onLoaded = onLoaded;
            _onFailed = onFailed;
        }

        // OnAdLoaded is NOT exposed as abstract in the C# binding — register manually via JNI
        [Java.Interop.Export("onAdLoaded")]
        public void OnAdLoaded(GmsInterstitialAd ad) => _onLoaded(ad);

        public override void OnAdFailedToLoad(GmsLoadError error) => _onFailed(error);
    }

    internal sealed class InterstitialContentCallback : FullScreenContentCallback
    {
        private readonly Action _onDismissed;
        private readonly Action<GmsAdError> _onFailed;

        public InterstitialContentCallback(Action onDismissed, Action<GmsAdError> onFailed)
        {
            _onDismissed = onDismissed;
            _onFailed = onFailed;
        }

        public override void OnAdDismissedFullScreenContent() => _onDismissed();
        public override void OnAdFailedToShowFullScreenContent(GmsAdError error) => _onFailed(error);
    }
}
