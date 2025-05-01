namespace RepeatList.Controls;

public partial class ActivityProgressBar : ContentView
{
    public static readonly BindableProperty IsRunningProperty = BindableProperty.Create(nameof(IsRunning), typeof(bool), typeof(ActivityProgressBar), default(bool));

    public bool IsRunning { get; set; }

	public ActivityProgressBar()
	{
		InitializeComponent();

        _ = AnimateProgressBar();
    }

    

    private async Task AnimateProgressBar()
    {
        while (IsRunning) // Läuft nur, wenn Visible = true
        {
            await LoadingBar.ProgressTo(1, 3000, Easing.Linear); // Füllt sich in 1 Sekunde
            await Task.Delay(1000); // Kleine Pause
            LoadingBar.Progress = 0; // Zurücksetzen
        }
    }

    // Falls sich `IsVisible` ändert, Animation starten oder stoppen
    //protected override void OnPropertyChanged(string propertyName = null)
    //{
    //    base.OnPropertyChanged(propertyName);

    //    if (propertyName == nameof(LoadingBar.IsVisible))
    //    {
    //        if (LoadingBar.IsVisible)
    //            _ = AnimateProgressBar(); // Starte die Animation
    //    }
    //}
}