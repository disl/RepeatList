using Android.Gms.Ads;
using Android.Gms.Ads.AppOpen;
using RepeatList.Services;

using GmsAppOpenAd = Android.Gms.Ads.AppOpen.AppOpenAd;
using GmsLoadError = Android.Gms.Ads.LoadAdError;
using GmsAdError = Android.Gms.Ads.AdError;
using static Android.Gms.Ads.AppOpen.AppOpenAd;

namespace RepeatList.Platforms.Android.Services
{
    public class AppOpenAdService : IAppOpenAdService
    {
        private const string AdUnitId = "ca-app-pub-9459821903521146/1100900659";
        private const int AdExpirationHours = 4;

        private GmsAppOpenAd? _appOpenAd;
        private DateTime _loadTime;
        private bool _isLoading;
        private bool _isShowing;

        public bool IsAdReady => _appOpenAd != null && !IsAdExpired;

        private bool IsAdExpired => (DateTime.UtcNow - _loadTime).TotalHours > AdExpirationHours;

        public AppOpenAdService()
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
                GmsAppOpenAd.Load(activity, AdUnitId, request, new AppOpenLoadCallback(
                    onLoaded: ad =>
                    {
                        _appOpenAd = ad;
                        _loadTime = DateTime.UtcNow;
                        _isLoading = false;
                    },
                    onFailed: _ =>
                    {
                        _appOpenAd = null;
                        _isLoading = false;
                    }
                ));
            });
        }

        public Task ShowAdIfReadyAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            if (!IsAdReady || _isShowing)
            {
                tcs.SetResult(false);
                return tcs.Task;
            }

            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity == null) { tcs.SetResult(false); return tcs.Task; }

            var ad = _appOpenAd;
            _appOpenAd = null;
            _isShowing = true;

            activity.RunOnUiThread(() =>
            {
                ad!.FullScreenContentCallback = new AppOpenContentCallback(
                    onDismissed: () =>
                    {
                        _isShowing = false;
                        LoadAd();
                        tcs.TrySetResult(true);
                    },
                    onFailed: _ =>
                    {
                        _isShowing = false;
                        LoadAd();
                        tcs.TrySetResult(false);
                    }
                );
                ad.Show(activity);
            });

            return tcs.Task;
        }
    }

    internal sealed class AppOpenLoadCallback : AppOpenAdLoadCallback
    {
        private readonly Action<GmsAppOpenAd> _onLoaded;
        private readonly Action<GmsLoadError> _onFailed;

        public AppOpenLoadCallback(Action<GmsAppOpenAd> onLoaded, Action<GmsLoadError> onFailed)
        {
            _onLoaded = onLoaded;
            _onFailed = onFailed;
        }

        // OnAdLoaded is NOT exposed as abstract in the C# binding — register manually via JNI
        [Java.Interop.Export("onAdLoaded")]
        public void OnAdLoaded(GmsAppOpenAd ad) => _onLoaded(ad);

        public override void OnAdFailedToLoad(GmsLoadError error) => _onFailed(error);
    }

    internal sealed class AppOpenContentCallback : FullScreenContentCallback
    {
        private readonly Action _onDismissed;
        private readonly Action<GmsAdError> _onFailed;

        public AppOpenContentCallback(Action onDismissed, Action<GmsAdError> onFailed)
        {
            _onDismissed = onDismissed;
            _onFailed = onFailed;
        }

        public override void OnAdDismissedFullScreenContent() => _onDismissed();
        public override void OnAdFailedToShowFullScreenContent(GmsAdError error) => _onFailed(error);
    }
}
