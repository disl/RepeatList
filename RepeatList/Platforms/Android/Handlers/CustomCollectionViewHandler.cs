using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls.Handlers.Items;

namespace RepeatList.Platforms.Android.Handlers
{
    public class CustomCollectionViewHandler : CollectionViewHandler
    {
        protected override RecyclerView CreatePlatformView()
        {
            var recyclerView = base.CreatePlatformView();

            // Android-spezifische Optimierungen
            recyclerView.SetItemViewCacheSize(40); // Mehr Caching
            recyclerView.HasFixedSize = true;      // Für gleichgroße Items
            recyclerView.SetItemAnimator(null);    // Animationen deaktivieren

            return recyclerView;
        }
    }
}
