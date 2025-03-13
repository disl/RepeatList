namespace RepeatList.Controls;

public partial class FlyoutHeader : ContentView
{
    public string AppVersion => $"Version {AppInfo.Current.VersionString}";

    public FlyoutHeader()
	{
		InitializeComponent();

        BindingContext = this;
    }
}