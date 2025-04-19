using Android.Gms.Ads;
using Microsoft.Maui.Handlers;
using RepeatList.Controls;

namespace RepeatList.Platforms.Android
{
    public class AdBannerViewHandler : ViewHandler<AdBannerView, AdView>
    {
        public AdBannerViewHandler() : base(ViewMapper)
        {
        }

        public static IPropertyMapper<AdBannerView, AdBannerViewHandler> ViewMapper = new PropertyMapper<AdBannerView, AdBannerViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(AdBannerView.AdUnitId)] = MapAdUnitId
        };

        protected override AdView CreatePlatformView()
        {
            var adView = new AdView(Context);
            adView.AdSize = AdSize.Banner;
            adView.AdUnitId = VirtualView.AdUnitId;
            adView.LoadAd(new AdRequest.Builder().Build());
            return adView;
        }

        public static void MapAdUnitId(AdBannerViewHandler handler, AdBannerView view)
        {
            if (handler.PlatformView != null)
            {
                //handler.PlatformView.AdUnitId = view.AdUnitId;
                handler.PlatformView.LoadAd(new AdRequest.Builder().Build());
            }
        }
    }
}
