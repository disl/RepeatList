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

            // Wichtig für kontinuierliche Erkennung
            _speechIntent.PutExtra(RecognizerIntent.ExtraPartialResults, true);

            // Timeout-Einstellungen anpassen
            _speechIntent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 2500);
            _speechIntent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 1500);
            _speechIntent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, 5000);

            // Mehr Ergebnisse anfordern
            _speechIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 10);

            // Optional: Sprach-Erkennung verbessern
            _speechIntent.PutExtra(RecognizerIntent.ExtraPreferOffline, false);
            _speechIntent.PutExtra(RecognizerIntent.ExtraPrompt, "Sprechen Sie jetzt...");
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

            string errorMessage = error switch
            {
                SpeechRecognizerError.NoMatch => "Keine Übereinstimmung gefunden - möglicherweise zu leise oder unverständlich",
                SpeechRecognizerError.Network => "Netzwerkfehler",
                SpeechRecognizerError.NetworkTimeout => "Netzwerk-Timeout",
                SpeechRecognizerError.Audio => "Audio-Fehler - Mikrofon nicht verfügbar",
                SpeechRecognizerError.Server => "Server-Fehler",
                SpeechRecognizerError.Client => "Client-Fehler",
                SpeechRecognizerError.SpeechTimeout => "Sprach-Timeout - zu lange Pause",
                SpeechRecognizerError.InsufficientPermissions => "Keine Berechtigung",
                SpeechRecognizerError.LanguageNotSupported => "Sprache nicht unterstützt",
                SpeechRecognizerError.LanguageUnavailable => "Sprache nicht verfügbar",
                _ => $"Unbekannter Fehler: {error}"
            };

            System.Diagnostics.Debug.WriteLine($"Detailed error: {errorMessage}");

            // Bei bestimmten Fehlern neu starten
            if (_shouldContinue &&
                (error == SpeechRecognizerError.NoMatch ||
                 error == SpeechRecognizerError.SpeechTimeout ||
                 error == SpeechRecognizerError.Client))
            {
                System.Diagnostics.Debug.WriteLine("Attempting to restart after error...");
                Task.Delay(500).ContinueWith(_ =>
                {
                    try
                    {
                        _speechRecognizer.StartListening(_speechIntent);
                        _isRecording = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Restart failed: {ex.Message}");
                    }
                });
            }
        }

        private void DebugBundleContents(Bundle bundle)
        {
            System.Diagnostics.Debug.WriteLine("=== Bundle Contents ===");
            foreach (var key in bundle.KeySet())
            {
                var value = bundle.Get(key);
                System.Diagnostics.Debug.WriteLine($"Key: {key}, Type: {value?.GetType().Name}, Value: {value}");
            }
            System.Diagnostics.Debug.WriteLine("=======================");
        }

        public void OnResults(Bundle? results)
        {
            if (results == null)
            {
                System.Diagnostics.Debug.WriteLine("OnResults: Bundle is null");
                return;
            }

            // Debug-Ausgabe
            DebugBundleContents(results);

            try
            {
                var matches = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition);

                if (matches == null)
                {
                    System.Diagnostics.Debug.WriteLine("OnResults: matches ArrayList is null");
                    // Alternative Methode versuchen
                    matches = GetResultsFromBundle(results);
                }

                if (matches != null && matches.Count > 0)
                {
                    var finalTranscription = matches[0];

                    if (!string.IsNullOrEmpty(finalTranscription))
                    {
                        if (_completeText.Length > 0)
                        {
                            _completeText.Append(" ");
                        }
                        _completeText.Append(finalTranscription);

                        TranscriptionReceived?.Invoke(this, _completeText.ToString());
                        System.Diagnostics.Debug.WriteLine($"OnResults: {finalTranscription}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("OnResults: No matches found");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnResults error: {ex.Message}");
            }

            // Automatisch neu starten
            if (_shouldContinue && _isRecording)
            {
                System.Diagnostics.Debug.WriteLine("Restarting recognition for continuous recording");
                // Kurze Pause bevor Neustart
                Task.Delay(100).ContinueWith(_ =>
                {
                    _speechRecognizer.StartListening(_speechIntent);
                });
            }
            else
            {
                _isRecording = false;
                CompleteTranscriptionReceived?.Invoke(this, _completeText.ToString().Trim());
            }
        }

        // Alternative Methode um Ergebnisse aus Bundle zu extrahieren
        private List<string>? GetResultsFromBundle(Bundle results)
        {
            try
            {
                // Verschiedene mögliche Keys probieren
                var matches = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition)
                            ?? results.GetStringArrayList("results")
                            ?? results.GetStringArrayList("matches");

                return matches.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetResultsFromBundle error: {ex.Message}");
                return null;
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
