using CommunityToolkit.Maui.Views;
using RepeatList.Models;
using RepeatList.Platforms.Android.Services;

namespace RepeatList;

public partial class SpeechRecognition : Popup<string>
{
    private HybridTranscriber _transcriber;
    private bool _isRecording = false;
    private string _finalText = string.Empty;


    private readonly ISpeechToText _speechToText;

    public string FinalText
    {
        get => _finalText;
        set
        {
            _finalText = value;
            OnPropertyChanged();
        }
    }

    public SpeechRecognition(ISpeechToText speechToText)
    {
        InitializeComponent();

        BindingContext = this;
        InitializeTranscriber();

        _speechToText = speechToText;
    }

    private void InitializeTranscriber()
    {
        _transcriber = new HybridTranscriber();
        _transcriber.TranscriptionReceived += OnTranscriptionReceived;
        _transcriber.CompleteTranscriptionReceived += OnCompleteTranscriptionReceived;
    }

    private async void OnRecordButtonClicked(object sender, EventArgs e)
    {
        if (!_isRecording)
        {
            // await StartRecording();

            try
            {
                // Mikrofon-Berechtigung prüfen
                var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Microphone>();
                }

                if (status == PermissionStatus.Granted)
                {
                    var result = await _speechToText.RecognizeSpeechAsync();
                    if (!string.IsNullOrEmpty(result))
                    {
                        await Application.Current.MainPage.DisplayAlert("Ergebnis", $"Erkannt: {result}", "OK");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Fehler", "Kein Text erkannt", "OK");
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Fehler", "Mikrofon-Berechtigung verweigert", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Fehler", ex.Message, "OK");
            }

        }
        else
        {
            StopRecording();
        }
    }

    private async Task StartRecording()
    {
        try
        {
            // Berechtigungen prüfen
            var hasPermission = await _transcriber.RequestPermissionsAsync();
            if (!hasPermission)
            {
                await Application.Current.MainPage.DisplayAlert("Berechtigung", "Mikrofon-Berechtigung wurde nicht erteilt", "OK");
                return;
            }

            // Aufnahme starten
            var result = await _transcriber.StartRecordingAsync();

            _isRecording = true;
            UpdateUIForRecordingState();

            StatusLabel.Text = "🎤 Aufnahme läuft... Sprechen Sie jetzt";
            RecordButton.Text = "⏹️ Stoppen";

            System.Diagnostics.Debug.WriteLine($"Recording started: {result}");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Fehler", $"Aufnahme konnte nicht gestartet werden: {ex.Message}", "OK");
            System.Diagnostics.Debug.WriteLine($"Start recording error: {ex.Message}");
        }
    }

    private void StopRecording()
    {
        try
        {
            _transcriber.StopRecording();
            _isRecording = false;
            UpdateUIForRecordingState();

            StatusLabel.Text = "Aufnahme beendet";
            RecordButton.Text = "🎤 Aufnahme starten";

            System.Diagnostics.Debug.WriteLine("Recording stopped");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Stop recording error: {ex.Message}");
        }
    }

    private void UpdateUIForRecordingState()
    {
        RecordButton.BackgroundColor = _isRecording ? Colors.Red : Colors.Green;

        // Optional: Button-Text animieren während Aufnahme
        if (_isRecording)
        {
            StartRecordingAnimation();
        }
        else
        {
            StopRecordingAnimation();
        }
    }

    private void StartRecordingAnimation()
    {
        // Einfache Animation für Aufnahme-Status
        var animation = new Animation();

        animation.Add(0, 0.5, new Animation(v => StatusLabel.Opacity = v, 1, 0.3));
        animation.Add(0.5, 1, new Animation(v => StatusLabel.Opacity = v, 0.3, 1));

        animation.Commit(StatusLabel, "RecordingAnimation", length: 1000, repeat: () => _isRecording);
    }

    private void StopRecordingAnimation()
    {
        StatusLabel.AbortAnimation("RecordingAnimation");
        StatusLabel.Opacity = 1;
    }

    // Event Handler für Transkription
    private void OnTranscriptionReceived(object sender, string transcription)
    {
        // Auf UI-Thread aktualisieren
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = $"Erkannt: {transcription}";

            // Optional: Live-Text anzeigen
            if (!string.IsNullOrEmpty(transcription))
            {
                ResultLabel.Text = transcription;
            }
        });
    }

    private void OnCompleteTranscriptionReceived(object sender, string transcription)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!string.IsNullOrEmpty(transcription))
            {
                FinalText = transcription;
                ResultLabel.Text = transcription;
                StatusLabel.Text = "Transkription abgeschlossen";

                System.Diagnostics.Debug.WriteLine($"Final transcription: {transcription}");
            }
            else
            {
                StatusLabel.Text = "Keine Sprache erkannt";
            }
        });
    }

    private void OnCopyListClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(FinalText))
        {
            Application.Current.MainPage.DisplayAlert("Hinweis", "Keine Transkription vorhanden", "OK");
            return;
        }

        // Popup mit Ergebnis schließen
        CloseAsync(FinalText);
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        // Aufnahme stoppen falls aktiv
        if (_isRecording)
        {
            StopRecording();
        }

        // Popup ohne Ergebnis schließen
        CloseAsync(null);
    }



    //protected override void OnDisappearing()
    //{
    //    // Aufräumen
    //    if (_isRecording)
    //    {
    //        StopRecording();
    //    }

    //    base.OnDisappearing();
    //}

    // Dispose Pattern für Ressourcen
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _transcriber?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~SpeechRecognition()
    {
        Dispose(false);
    }




    //-----------------------------------------------------------------------------------------------

    //private readonly AudioRecorderService _audioRecorder;    
    //private readonly DeepSeekClient _deepSeekClient;
    //private readonly PositionsPageViewModel _positionsPageViewModel;

    //private bool _isRecording = false;
    //private readonly IAudioTranscriber _transcriber;
    //private string _transcribedText;

    //private HybridTranscriber _transcriber;
    //private bool _isRecording = false;
    //private string _finalText = string.Empty;


    //public SpeechRecognition()
    //{
    //    InitializeComponent();
    //    _audioRecorder = new AudioRecorderService();
    //    _deepSeekClient = new DeepSeekClient();

    //    _transcriber = new AndroidTranscriber();
    //    _positionsPageViewModel = new PositionsPageViewModel(_transcriber);
    //}

    //private async void OnRecordButtonClicked(object sender, EventArgs e)
    //{
    //    try
    //    {
    //        if (_isRecording)
    //        {
    //            // Aufnahme stoppen
    //            await StopRecordingAsync();
    //        }
    //        else
    //        {
    //            // Aufnahme starten
    //            await StartRecordingAsync();
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        await Application.Current.MainPage.DisplayAlert("Fehler", ex.Message, "OK");
    //    }
    //}



    //private async Task StartRecordingAsync()
    //{
    //    try
    //    {



    //        //// Nur Mikrofon-Berechtigung prüfen
    //        var status = await Permissions.RequestAsync<Permissions.Microphone>();
    //        if (status != PermissionStatus.Granted)
    //        {
    //            await Application.Current.MainPage.DisplayAlert("Berechtigung", "Mikrofon-Zugriff benötigt", "OK");
    //            return;
    //        }

    //        ////_audioRecorder.StartRecording();
    //        ///
    //        await _positionsPageViewModel.StartTranscription();

    //        ////_audioRecorder = new AudioRecorderService();
    //        //_audioRecorder.StartRecording();

    //        _isRecording = true;

    //        //// UI aktualisieren
    //        RecordButton.Text = "⏹️ Stoppen";
    //        StatusLabel.Text = "🎤 Aufnahme läuft...";
    //        RecordButton.BackgroundColor = Colors.Red;
    //    }
    //    catch (Exception ex)
    //    {
    //        await Application.Current.MainPage.DisplayAlert("Fehler", ex.Message, "OK");
    //    }
    //}

    //private async Task StopRecordingAsync()
    //{
    //    try
    //    {
    //         _positionsPageViewModel.StopTranscription();

    //        //var audioFilePath = _audioRecorder.StopRecording();

    //        _audioRecorder.StopRecording();
    //        //string audioFilePath = _audioRecorder.GetFilePath();

    //        _isRecording = false;

    //        // UI aktualisieren
    //        RecordButton.Text = "🎤 Aufnahme starten";
    //        //StatusLabel.Text = "🔄 Verarbeite Audio...";
    //        _positionsPageViewModel.TranscribedText = "🔄 Verarbeite Audio...";
    //        RecordButton.BackgroundColor = Colors.Green;

    //        // Audio verarbeiten
    //        //await ProcessAudioFile(audioFilePath);



    //        ////var audioFilePath = _audioRecorder.StopRecording();

    //        //_audioRecorder.StopRecording();
    //        //string audioFilePath = _audioRecorder.GetFilePath();

    //        //_isRecording = false;

    //        //// UI aktualisieren
    //        //RecordButton.Text = "🎤 Aufnahme starten";
    //        //StatusLabel.Text = "🔄 Verarbeite Audio...";
    //        //RecordButton.BackgroundColor = Colors.Green;

    //        //// Audio verarbeiten
    //        //await ProcessAudioFile(audioFilePath);
    //    }
    //    catch (Exception ex)
    //    {
    //        await Application.Current.MainPage.DisplayAlert("Fehler", ex.Message, "OK");
    //    }
    //}

    //private async Task ProcessAudioFile(string audioFilePath)
    //{
    //    if (File.Exists(audioFilePath))
    //    {
    //        var fileInfo = new FileInfo(audioFilePath);
    //        //StatusLabel.Text = $"✅ Audio gespeichert ({fileInfo.Length / 1024} KB)";


    //        var new_list = await _deepSeekClient.TranscribeToShoppingList(audioFilePath);

    //        // Hier Ihre Speech-to-Text Logik
    //        // await YourSpeechToTextService.ProcessAsync(audioFilePath);
    //    }
    //}

    //async Task CloseMe(dynamic param)
    //{
    //    // Popup zuerst schließen
    //    if (Handler != null)
    //    {
    //        CloseAsync(param); // Bei Popup<TResult> -> kein await, sofort Ergebnis setzen
    //    }

    //    // Danach Navigation
    //    if (Navigation.ModalStack.Any())
    //    {
    //        await Navigation.PopModalAsync();
    //    }


    //}

    //private async void OnCancelClicked(object sender, EventArgs e)
    //{
    //    await CloseMe("");
    //}

    ////private async Task OnStopButtonClicked(object sender, EventArgs e)
    ////{
    ////    await StopRecordingAsync();
    ////}

    //private void OnStopButtonClicked(object sender, EventArgs e)
    //{
    //    _ =StopRecordingAsync();
    //}

    //private void OnCopyListClicked(object sender, EventArgs e)
    //{

    //}
}