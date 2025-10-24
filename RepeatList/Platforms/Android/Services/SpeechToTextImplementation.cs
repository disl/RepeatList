using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Speech;
using RepeatList.Models;

namespace RepeatList.Platforms.Android.Services
{
    public class SpeechToTextImplementation : Java.Lang.Object, ISpeechToText, IRecognitionListener
    {
        private readonly Activity _activity;
        private SpeechRecognizer _recognizer;
        private readonly TaskCompletionSource<string> _tcs = new();

        public SpeechToTextImplementation(IMauiContext mauiContext)
        {
            _activity = mauiContext?.Services?.GetService<Activity>()
                ?? throw new InvalidOperationException("Keine Android Activity verfügbar.");
            _recognizer = SpeechRecognizer.CreateSpeechRecognizer(_activity);
            _recognizer.SetRecognitionListener(this);
        }

        public async Task<string> RecognizeSpeechAsync()
        {
            if (_recognizer == null)
            {
                throw new InvalidOperationException("SpeechRecognizer nicht initialisiert.");
            }

            var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            intent.PutExtra(RecognizerIntent.ExtraLanguage, "de-DE");
            intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
            intent.PutExtra(RecognizerIntent.ExtraPartialResults, false);

            _tcs.TrySetResult(null); // TaskCompletionSource zurücksetzen
            _recognizer.StartListening(intent);

            return await _tcs.Task;
        }

        // IRecognitionListener-Methoden
        public void OnReadyForSpeech(Bundle? parameters) { }
        public void OnBeginningOfSpeech() { }
        public void OnRmsChanged(float rmsdB) { }
        public void OnBufferReceived(byte[] buffer) { }
        public void OnEndOfSpeech() { }

        public void OnError([GeneratedEnum] SpeechRecognizerError error)
        {
            _tcs.TrySetException(new Exception($"Fehler bei der Spracherkennung: {error}"));
        }

        public void OnEvent(int eventType, Bundle? @params) { }
        public void OnPartialResults(Bundle? results) { }

        public void OnResults(Bundle? results)
        {
            var matches = results?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches?.Count > 0)
            {
                _tcs.TrySetResult(matches[0]);
            }
            else
            {
                _tcs.TrySetException(new Exception("Keine Ergebnisse gefunden"));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _recognizer?.Dispose();
                _recognizer = null;
            }
            base.Dispose(disposing);
        }
    }
}
