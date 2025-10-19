using CommunityToolkit.Maui.Views;
using RepeatList.Services;

namespace RepeatList;

public partial class SpeechRecognition : Popup<string>
{
    private readonly AudioRecorderService _audioRecorder;
    private readonly DeepSeekClient _deepSeekClient;
    private bool _isRecording = false;

    public SpeechRecognition()
    {
        InitializeComponent();
        _audioRecorder = new AudioRecorderService();
        _deepSeekClient = new DeepSeekClient("sk-a3240964efda4aa1aa6cf6ffcf9713b2");
    }

    private async void OnRecordButtonClicked(object sender, EventArgs e)
    {
        try
        {
            // Berechtigung prüfen
            var status = await Permissions.RequestAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                await Application.Current.MainPage.DisplayAlert("Fehler", "Mikrofon-Berechtigung benötigt", "OK");
                return;
            }

            // Aufnahme starten
            _audioRecorder.StartRecording();
            _isRecording = true;

            RecordButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            StatusLabel.Text = "🎤 Aufnahme läuft... sprich jetzt!";
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Fehler", ex.Message, "OK");
        }
    }

    private async void OnStopButtonClicked(object sender, EventArgs e)
    {
        if (!_isRecording) return;

        try
        {
            // Aufnahme stoppen
            var audioFilePath = _audioRecorder.StopRecording();
            _isRecording = false;

            RecordButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            StatusLabel.Text = "🔄 Verarbeite Sprache...";

            // Transkribieren mit DeepSeek
            var shoppingList = await _deepSeekClient.TranscribeToShoppingList(audioFilePath);

            // Ergebnis anzeigen
            ResultLabel.Text = shoppingList;
            StatusLabel.Text = "✅ Liste erstellt!";
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Fehler", ex.Message, "OK");
            StatusLabel.Text = "Fehler bei Verarbeitung";
        }
    }

    //private void OnAddManualItem(object sender, EventArgs e)
    //{
    //    if (!string.IsNullOrWhiteSpace(InputEntry.Text))
    //    {
    //        ResultLabel.Text = InputEntry.Text;
    //        InputEntry.Text = string.Empty;
    //    }
    //}

    private async void OnCopyListClicked(object sender, EventArgs e)
    {
        await CloseMe(ResultLabel.Text);

        //await Clipboard.Default.SetTextAsync(ResultLabel.Text);
        //await Application.Current.MainPage.DisplayAlert("Erfolg", "Liste kopiert!", "OK");
    }

    async Task CloseMe(dynamic param)
    {
        // Popup zuerst schließen
        if (Handler != null)
        {
            CloseAsync(param); // Bei Popup<TResult> -> kein await, sofort Ergebnis setzen
        }

        // Danach Navigation
        if (Navigation.ModalStack.Any())
        {
            await Navigation.PopModalAsync();
        }

        
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await CloseMe("");
    }
}