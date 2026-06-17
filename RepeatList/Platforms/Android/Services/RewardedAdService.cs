using Android.Gms.Ads;
using Android.Gms.Ads.Rewarded;
using RepeatList.Services;
using Activity = Android.App.Activity;

// Aliases to avoid namespace conflict with RepeatList.Platforms.Android.*
using GmsRewardedAd = Android.Gms.Ads.Rewarded.RewardedAd;
using GmsLoadError = Android.Gms.Ads.LoadAdError;
using GmsAdError = Android.Gms.Ads.AdError;
using GmsRewardItem = Android.Gms.Ads.Rewarded.IRewardItem;

namespace RepeatList.Platforms.Android.Services
{
    public class RewardedAdService : IRewardedAdService
    {
        private const string AdUnitId = "ca-app-pub-9459821903521146/4027436573";

        private GmsRewardedAd? _rewardedAd;
        private bool _isLoading;

        public bool IsAdReady => _rewardedAd != null;

        public RewardedAdService()
        {
            LoadAd();
        }

        private void LoadAd()
        {
            if (_isLoading) return;
            _isLoading = true;

            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity == null) return;

            activity.RunOnUiThread(() =>
            {
                var request = new AdRequest.Builder().Build();
                GmsRewardedAd.Load(activity, AdUnitId, request, new InternalLoadCallback(
                    onLoaded: ad =>
                    {
                        _rewardedAd = ad;
                        _isLoading = false;
                    },
                    onFailed: _ =>
                    {
                        _rewardedAd = null;
                        _isLoading = false;
                    }
                ));
            });
        }

        public Task<bool> ShowRewardedAdAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

            if (_rewardedAd == null || activity == null)
            {
                tcs.SetResult(false);
                return tcs.Task;
            }

            var ad = _rewardedAd;
            _rewardedAd = null;

            activity.RunOnUiThread(() =>
            {
                ad.FullScreenContentCallback = new InternalContentCallback(
                    onDismissed: () =>
                    {
                        if (!tcs.Task.IsCompleted)
                            tcs.SetResult(false);
                    },
                    onFailed: _ =>
                    {
                        if (!tcs.Task.IsCompleted)
                            tcs.SetResult(false);
                        LoadAd();
                    }
                );

                ad.Show(activity, new InternalRewardListener(reward =>
                {
                    tcs.SetResult(true);
                    LoadAd();
                }));
            });

            return tcs.Task;
        }
    }

    internal sealed class InternalLoadCallback : RewardedAdLoadCallback
    {
        private readonly Action<GmsRewardedAd> _onLoaded;
        private readonly Action<GmsLoadError> _onFailed;

        public InternalLoadCallback(Action<GmsRewardedAd> onLoaded, Action<GmsLoadError> onFailed)
        {
            _onLoaded = onLoaded;
            _onFailed = onFailed;
        }

        // OnAdLoaded is NOT exposed as abstract in the C# binding — register manually via JNI
        [Java.Interop.Export("onAdLoaded")]
        public void OnAdLoaded(GmsRewardedAd p0) => _onLoaded(p0);

        public override void OnAdFailedToLoad(GmsLoadError p0) => _onFailed(p0);
    }

    internal sealed class InternalRewardListener : Java.Lang.Object, IOnUserEarnedRewardListener
    {
        private readonly Action<GmsRewardItem> _onReward;

        public InternalRewardListener(Action<GmsRewardItem> onReward) => _onReward = onReward;

        public void OnUserEarnedReward(GmsRewardItem reward) => _onReward(reward);
    }

    internal sealed class InternalContentCallback : FullScreenContentCallback
    {
        private readonly Action _onDismissed;
        private readonly Action<GmsAdError> _onFailed;

        public InternalContentCallback(Action onDismissed, Action<GmsAdError> onFailed)
        {
            _onDismissed = onDismissed;
            _onFailed = onFailed;
        }

        public override void OnAdDismissedFullScreenContent() => _onDismissed();
        public override void OnAdFailedToShowFullScreenContent(GmsAdError p0) => _onFailed(p0);
    }
}
