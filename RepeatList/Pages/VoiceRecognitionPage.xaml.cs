using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Speech;
using CommunityToolkit.Maui.Views;
using System.Text;

namespace RepeatList;

public partial class VoiceRecognitionPage : Popup<string>
{

    bool isListening = false;
    bool IsListening
    {
        get { return isListening; }

        set
        {
            isListening = value;

            startButton.IsVisible = !isListening;
            stopButton.IsVisible = isListening;
        }
    }

#if ANDROID
    SpeechRecognizer? recognizer;
    Intent? voiceIntent;
    StringBuilder textBuilder = new();

#endif

    public VoiceRecognitionPage()
    {
        InitializeComponent();
    }

    private async void OnStartListeningClicked(object sender, EventArgs e)
    {
#if ANDROID
        if (IsListening)
            return;

        if (!await CheckMicrophonePermissionAsync())
        {
            await Application.Current.MainPage.DisplayAlert("Zugriff verweigert", "Mikrofonzugriff ist erforderlich.", "OK");
            return;
        }

        textBuilder.Clear();
        txtResult.Text = string.Empty;


        var context = Platform.CurrentActivity ?? Android.App.Application.Context;

        voiceIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
        voiceIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
        voiceIntent.PutExtra(RecognizerIntent.ExtraPrompt, "Sprich jetzt...");
        voiceIntent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.Default);
        voiceIntent.PutExtra(RecognizerIntent.ExtraPartialResults, true);
        voiceIntent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 15000);
        voiceIntent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 15000);

        var listener = new ContinuousSpeechListener(OnTextRecognized, RestartListening);
        recognizer = SpeechRecognizer.CreateSpeechRecognizer(context);
        recognizer.SetRecognitionListener(listener);

        IsListening = true;
        recognizer.StartListening(voiceIntent);
        //await Application.Current.MainPage.DisplayAlert("Info", "Du kannst jetzt deine Einkaufsliste diktieren.", "OK");
#else
            await DisplayAlert("Nicht unterstützt", "Nur auf Android verfügbar.", "OK");
#endif
    }

    private async void OnStopListeningClicked(object sender, EventArgs e)
    {
#if ANDROID
    IsListening = false;

    try
    {
        recognizer?.StopListening();
    }
    catch { }

    try
    {
        recognizer?.Cancel();
    }
    catch { }

    try
    {
        recognizer?.Destroy();
    }
    catch (Exception ex)
    {
        Android.Util.Log.Warn("Speech", "Destroy failed: " + ex.Message);
    }

    recognizer = null;

    if (!string.IsNullOrEmpty(txtResult.Text))
    {
        await CloseMe(txtResult.Text);
    }
#endif
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

#if ANDROID
    private void OnTextRecognized(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!textBuilder.ToString().EndsWith(text))
                textBuilder.AppendLine(text);

            txtResult.Text = textBuilder.ToString();
        });
    }

    private void RestartListening()
    {
        if (!IsListening || voiceIntent == null) return;
        recognizer?.StartListening(voiceIntent);
    }
#endif

    private async Task<bool> CheckMicrophonePermissionAsync()
    {
#if ANDROID
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.Microphone>();
        return status == PermissionStatus.Granted;
#else
            return false;
#endif
    }


#if ANDROID
    public class ContinuousSpeechListener : Java.Lang.Object, IRecognitionListener
    {
        private readonly Action<string> _onResult;
        private readonly Action _onEnd;

        public ContinuousSpeechListener(Action<string> onResult, Action onEnd)
        {
            _onResult = onResult;
            _onEnd = onEnd;
        }

        public void OnReadyForSpeech(Bundle? @params) { }
        public void OnBeginningOfSpeech() { }
        public void OnRmsChanged(float rmsdB) { }
        public void OnBufferReceived(byte[]? buffer) { }
        public void OnEndOfSpeech() => _onEnd.Invoke();

        public void OnError([GeneratedEnum] SpeechRecognizerError error)
        {
            // bestimmte Fehler ignorieren und neu starten
            if (error == SpeechRecognizerError.NoMatch || error == SpeechRecognizerError.SpeechTimeout)
                _onEnd.Invoke();
        }

        public void OnResults(Bundle? results)
        {
            var matches = results?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches != null && matches.Count > 0)
                _onResult.Invoke(matches[0]);
        }

        public void OnPartialResults(Bundle? partialResults)
        {
            var matches = partialResults?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches != null && matches.Count > 0)
                _onResult.Invoke(matches[0]);
        }

        public void OnEvent(int eventType, Bundle? @params) { }
    }
#endif
}
