namespace RepeatList.Controls;

public partial class FlyoutHeader : ContentView
{
    private string DeviceId;
    public string AppVersion => $"Version {AppInfo.Current.VersionString}";

    // Mindest-Abstand von der Oberkante (in dp), den der Header frei lassen muss,
    // damit er die Statusleiste nicht überdeckt. ~15 mm entsprechen ungefähr 57 dp.
    private const double MinTopInsetDp = 57.0;

    public FlyoutHeader()
	{
		InitializeComponent();

        BindingContext = this;

        ApplyStatusBarInset();
	}

    // Der Flyout-Header wird ganz oben im Shell-Drawer gerendert und beginnt sonst bei
    // Y=0 — also hinter der Android-Statusleiste. Hier wird die Statusleistenhöhe zuverlässig
    // ermittelt (nicht über WindowInsets, das im Konstruktor noch null ist) und als Top-Padding
    // des blauen Containers gesetzt, mindestens aber MinTopInsetDp. Die ContentView-Höhe wird
    // dabei mit erhöht, damit der Header-Inhalt (Labels) trotz Inset sichtbar bleibt.
    private const double HeaderContentHeightDp = 50.0;

    private void ApplyStatusBarInset()
    {
#if ANDROID
        try
        {
            double topDp = MinTopInsetDp;

            var activity = Platform.CurrentActivity;
            var density = activity?.Resources?.DisplayMetrics?.Density ?? 1f;

            // Statusleistenhöhe über die native Ressourcen-Id ermitteln (zuverlässig,
            // unabhängig vom WindowInsets-Timing). Fallback auf getesteten Mindestwert.
            var context = activity ?? Android.App.Application.Context;
            int resourceId = context.Resources.GetIdentifier("status_bar_height", "dimen", "android");
            if (resourceId > 0)
            {
                var px = context.Resources.GetDimensionPixelSize(resourceId);
                var measuredDp = px / density;
                if (measuredDp > 0)
                    topDp = Math.Max(measuredDp, MinTopInsetDp);
            }

            HeaderRoot.Padding = new Thickness(HeaderRoot.Padding.Left, topDp, HeaderRoot.Padding.Right, HeaderRoot.Padding.Bottom);

            // Gesamthöhe = Inhalt + Statusleisten-Abstand, damit nichts kollabiert oder
            // der Text verdeckt wird.
            HeightRequest = HeaderContentHeightDp + topDp;
        }
        catch (Exception)
        {
            // Insets sind hier rein kosmetisch — kein Grund, den Header unmöglich zu machen.
        }
#endif
    }
}