using CommunityToolkit.Maui.Views;
using RepeatList.Models;
using RepeatList.Platforms.Android.Services;
using RepeatList.Services;
using RepeatList.ViewModels;

namespace RepeatList;

public partial class SpeechRecognition : Popup<string>
{
    private readonly AudioRecorderService _audioRecorder;    
    private readonly DeepSeekClient _deepSeekClient;
    private readonly PositionsPageViewModel _positionsPageViewModel;

    private bool _isRecording = false;
    private readonly IAudioTranscriber _transcriber;
    private string _transcribedText;


    public SpeechRecognition()
    {
        InitializeComponent();
        _audioRecorder = new AudioRecorderService();
        _deepSeekClient = new DeepSeekClient("sk-a3240964efda4aa1aa6cf6ffcf9713b2");

        _transcriber = new AndroidTranscriber();
        _positionsPageViewModel = new PositionsPageViewModel(_transcriber);
    }

    private async void OnRecordButtonClicked(object sender, EventArgs e)
    {
        try
        {
            if (_isRecording)
            {
                // Aufnahme stoppen
                await StopRecordingAsync();
            }
            else
            {
                // Aufnahme starten
                await StartRecordingAsync();
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Fehler", ex.Message, "OK");
        }
    }

 

    private async Task StartRecordingAsync()
    {
        try
        {



            //// Nur Mikrofon-Berechtigung prüfen
            var status = await Permissions.RequestAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                await Application.Current.MainPage.DisplayAlert("Berechtigung", "Mikrofon-Zugriff benötigt", "OK");
                return;
            }

            ////_audioRecorder.StartRecording();
            ///
            await _positionsPageViewModel.StartTranscription();

            ////_audioRecorder = new AudioRecorderService();
            //_audioRecorder.StartRecording();

            _isRecording = true;

            //// UI aktualisieren
            RecordButton.Text = "⏹️ Stoppen";
            StatusLabel.Text = "🎤 Aufnahme läuft...";
            RecordButton.BackgroundColor = Colors.Red;
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Fehler", ex.Message, "OK");
        }
    }

    private async Task StopRecordingAsync()
    {
        try
        {
             _positionsPageViewModel.StopTranscription();

            //var audioFilePath = _audioRecorder.StopRecording();

            _audioRecorder.StopRecording();
            //string audioFilePath = _audioRecorder.GetFilePath();

            _isRecording = false;

            // UI aktualisieren
            RecordButton.Text = "🎤 Aufnahme starten";
            //StatusLabel.Text = "🔄 Verarbeite Audio...";
            _positionsPageViewModel.TranscribedText = "🔄 Verarbeite Audio...";
            RecordButton.BackgroundColor = Colors.Green;

            // Audio verarbeiten
            //await ProcessAudioFile(audioFilePath);



            ////var audioFilePath = _audioRecorder.StopRecording();

            //_audioRecorder.StopRecording();
            //string audioFilePath = _audioRecorder.GetFilePath();

            //_isRecording = false;

            //// UI aktualisieren
            //RecordButton.Text = "🎤 Aufnahme starten";
            //StatusLabel.Text = "🔄 Verarbeite Audio...";
            //RecordButton.BackgroundColor = Colors.Green;

            //// Audio verarbeiten
            //await ProcessAudioFile(audioFilePath);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Fehler", ex.Message, "OK");
        }
    }

    private async Task ProcessAudioFile(string audioFilePath)
    {
        if (File.Exists(audioFilePath))
        {
            var fileInfo = new FileInfo(audioFilePath);
            //StatusLabel.Text = $"✅ Audio gespeichert ({fileInfo.Length / 1024} KB)";


            var new_list = await _deepSeekClient.TranscribeToShoppingList(audioFilePath);

            // Hier Ihre Speech-to-Text Logik
            // await YourSpeechToTextService.ProcessAsync(audioFilePath);
        }
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

    //private async Task OnStopButtonClicked(object sender, EventArgs e)
    //{
    //    await StopRecordingAsync();
    //}

    private void OnStopButtonClicked(object sender, EventArgs e)
    {
        _ =StopRecordingAsync();
    }

    private void OnCopyListClicked(object sender, EventArgs e)
    {

    }
}