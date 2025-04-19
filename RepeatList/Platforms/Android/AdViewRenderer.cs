using Android.Content;
using Android.Gms.Ads;
using Android.Widget;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform;
using RepeatList.Controls;

[assembly: ExportRenderer(typeof(RepeatList.Controls.AdBanner), typeof(RepeatList.Platforms.Android.AdViewRenderer))]
namespace RepeatList.Platforms.Android
{
    public class AdViewRenderer : ViewRenderer<AdBanner, AdView>
    {
        public AdViewRenderer(Context context) : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<AdBanner> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                var adView = new AdView(Context)
                {
                    AdSize = AdSize.Banner,
                    AdUnitId = "ca-app-pub-3940256099942544/6300978111" // Test-ID
                };

                var adRequest = new AdRequest.Builder().Build();
                adView.LoadAd(adRequest);

                SetNativeControl(adView);
            }
        }
    }
}