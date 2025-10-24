using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Speech;
using RepeatList.Models;


namespace RepeatList.Platforms.Android.Services
{
    public class AndroidTranscriber : Java.Lang.Object, IAudioTranscriber, IRecognitionListener
    {
        private SpeechRecognizer _speechRecognizer;
        private Intent _speechIntent;
        private bool _isRecording = false;
        private System.Text.StringBuilder _completeText = new System.Text.StringBuilder();

        public bool IsRecording => _isRecording;
        public event EventHandler<string> TranscriptionReceived;
        public event EventHandler<string> CompleteTranscriptionReceived;
        private bool _shouldContinue = true; // Für kontinuierliche Aufnahme


        public AndroidTranscriber()
        {
            InitializeSpeechRecognizer();
        }

        private void InitializeSpeechRecognizer()
        {
            if (!SpeechRecognizer.IsRecognitionAvailable(Platform.AppContext))
            {
                System.Diagnostics.Debug.WriteLine("Speech recognition not available!");
                return;
            }

            _speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(Platform.AppContext);
            _speechRecognizer.SetRecognitionListener(this);

            _speechIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            _speechIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            _speechIntent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.German);
            _speechIntent.PutExtra(RecognizerIntent.ExtraCallingPackage, Platform.AppContext.PackageName);
            _speechIntent.PutExtra(RecognizerIntent.ExtraPartialResults, true);

            // Wichtig für komplette Ergebnisse

            _speechIntent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 5000);  // 5 Sekunden
            _speechIntent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 3000);  // 3 Sekunden
            _speechIntent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, 10000);  // 10 Sekunden Minimum


            _speechIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 5);
        }

        public async Task<bool> RequestPermissionsAsync()
        {
            var status = await Permissions.RequestAsync<Permissions.Microphone>();
            return status == PermissionStatus.Granted;
        }

        public Task<string> StartRecordingAsync()
        {
            if (!_isRecording)
            {
                _completeText.Clear();
                _shouldContinue = true;
                _speechRecognizer.StartListening(_speechIntent);
                _isRecording = true;
            }
            return Task.FromResult("Recording started");
        }

        public void StopRecording()
        {
            if (_isRecording)
            {
                _shouldContinue = false; // Kein Auto-Restart mehr
                _speechRecognizer.StopListening();
                _isRecording = false;

                var finalText = _completeText.ToString().Trim();
                if (!string.IsNullOrEmpty(finalText))
                {
                    CompleteTranscriptionReceived?.Invoke(this, finalText);
                }
            }
        }

        // IRecognitionListener Implementation
        public void OnReadyForSpeech(Bundle? @params)
        {
            System.Diagnostics.Debug.WriteLine("Ready for speech");
            _completeText.Clear();
        }

        public void OnBeginningOfSpeech()
        {
            System.Diagnostics.Debug.WriteLine("Beginning of speech");
        }

        public void OnRmsChanged(float rmsdB) { }

        public void OnBufferReceived(byte[]? buffer) { }

        public void OnEndOfSpeech()
        {
            System.Diagnostics.Debug.WriteLine("End of speech");
        }

        public void OnError([GeneratedEnum] SpeechRecognizerError error)
        {
            _isRecording = false;
            System.Diagnostics.Debug.WriteLine($"Speech recognition error: {error}");

            // Bei bestimmten Fehlern automatisch neu starten
            if (_shouldContinue && _isRecording &&
                (error == SpeechRecognizerError.NoMatch ||
                 error == SpeechRecognizerError.SpeechTimeout))
            {
                System.Diagnostics.Debug.WriteLine("Restarting after error");
                System.Threading.Thread.Sleep(100); // Kurze Pause
                _speechRecognizer.StartListening(_speechIntent);
            }
            else
            {
                _isRecording = false;
            }

            // Detaillierte Error-Meldungen
            string errorMessage = error switch
            {
                SpeechRecognizerError.NoMatch => "Keine Übereinstimmung gefunden",
                SpeechRecognizerError.Network => "Netzwerkfehler",
                SpeechRecognizerError.NetworkTimeout => "Netzwerk-Timeout",
                SpeechRecognizerError.Audio => "Audio-Fehler",
                SpeechRecognizerError.Server => "Server-Fehler",
                SpeechRecognizerError.Client => "Client-Fehler",
                SpeechRecognizerError.SpeechTimeout => "Sprach-Timeout",
                SpeechRecognizerError.InsufficientPermissions => "Keine Berechtigung",
                _ => $"Unbekannter Fehler: {error}"
            };

            System.Diagnostics.Debug.WriteLine(errorMessage);
        }

        public void OnResults(Bundle? results)
        {
            var matches = results?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches != null && matches.Count > 0)
            {
                var finalTranscription = matches[0];

                if (!string.IsNullOrEmpty(finalTranscription))
                {
                    // Text anfügen statt zu ersetzen
                    if (_completeText.Length > 0)
                    {
                        _completeText.Append(" ");
                    }
                    _completeText.Append(finalTranscription);

                    // Zwischenergebnis senden
                    TranscriptionReceived?.Invoke(this, _completeText.ToString());
                }
            }

            // Automatisch neu starten wenn noch am Aufnehmen
            if (_shouldContinue && _isRecording)
            {
                System.Diagnostics.Debug.WriteLine("Restarting recognition for continuous recording");
                _speechRecognizer.StartListening(_speechIntent);
            }
            else
            {
                _isRecording = false;
                // Finales Ergebnis senden
                CompleteTranscriptionReceived?.Invoke(this, _completeText.ToString().Trim());
            }
        }

        //public void OnResults(Bundle? results)
        //{
        //    var matches = results?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
        //    if (matches != null && matches.Count > 0)
        //    {
        //        // Das beste Ergebnis nehmen
        //        var finalTranscription = matches[0];

        //        if (!string.IsNullOrEmpty(finalTranscription))
        //        {
        //            _completeText.Clear();
        //            _completeText.Append(finalTranscription);

        //            // Finales Ergebnis senden
        //            CompleteTranscriptionReceived?.Invoke(this, finalTranscription);
        //            TranscriptionReceived?.Invoke(this, finalTranscription);
        //        }
        //    }
        //    _isRecording = false;
        //}

        public void OnPartialResults(Bundle? partialResults)
        {
            var matches = partialResults?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches != null && matches.Count > 0)
            {
                var partialTranscription = matches[0];
                if (!string.IsNullOrEmpty(partialTranscription))
                {
                    // Partielles Ergebnis senden (für Live-Updates)
                    TranscriptionReceived?.Invoke(this, partialTranscription);
                }
            }
        }

        public void OnEvent(int eventType, Bundle? @params) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _speechRecognizer?.StopListening();
                _speechRecognizer?.Destroy();
                _speechRecognizer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
