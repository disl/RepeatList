using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Speech;
using RepeatList.Models;

namespace RepeatList.Platforms.Android.Services
{
    public class HybridTranscriber : Java.Lang.Object, IAudioTranscriber, IRecognitionListener
    {
        private MediaRecorder _mediaRecorder;
        private SpeechRecognizer _speechRecognizer;
        private Intent _speechIntent;
        private bool _isRecording = false;
        private string _audioFilePath;

        public bool IsRecording => _isRecording;
        public event EventHandler<string> TranscriptionReceived;
        public event EventHandler<string> CompleteTranscriptionReceived;
        private string backup_audio_file = "backup_audio.3gp";

        private TaskCompletionSource<string> _transcriptionTask;



        public HybridTranscriber()
        {
            InitializeMediaRecorder();
            InitializeSpeechRecognizer();
        }

        private void InitializeMediaRecorder()
        {
            _mediaRecorder = new MediaRecorder();
        }

        private void InitializeSpeechRecognizer()
        {
            if (SpeechRecognizer.IsRecognitionAvailable(Platform.AppContext))
            {
                _speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(Platform.AppContext);
                _speechRecognizer.SetRecognitionListener(this);

                _speechIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                _speechIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
                _speechIntent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.German);
                _speechIntent.PutExtra(RecognizerIntent.ExtraPartialResults, true);
                _speechIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 5);
            }
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
                StartMediaRecording();
                StartSpeechRecognition();
                _isRecording = true;
            }
            return Task.FromResult("Hybrid recording started");
        }

        private void StartMediaRecording()
        {
            try
            {
                _audioFilePath = Path.Combine(Path.GetTempPath(), $"{backup_audio_file}");

                if (File.Exists(_audioFilePath))
                {
                    File.Delete(_audioFilePath);
                }

                _mediaRecorder.SetAudioSource(AudioSource.Mic);
                _mediaRecorder.SetOutputFormat(OutputFormat.ThreeGpp);
                _mediaRecorder.SetAudioEncoder(AudioEncoder.AmrNb);
                _mediaRecorder.SetOutputFile(_audioFilePath);
                _mediaRecorder.Prepare();
                _mediaRecorder.Start();

                System.Diagnostics.Debug.WriteLine("MediaRecorder started");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MediaRecorder error: {ex.Message}");
            }
        }

        private void StartSpeechRecognition()
        {
            try
            {
                _speechRecognizer?.StartListening(_speechIntent);
                System.Diagnostics.Debug.WriteLine("SpeechRecognizer started");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SpeechRecognizer error: {ex.Message}");
            }
        }

        public void StopRecording()
        {
            if (_isRecording)
            {
                StopSpeechRecognition();
                StopMediaRecording();
                _isRecording = false;
            }
        }

        private void StopMediaRecording()
        {
            try
            {
                _mediaRecorder?.Stop();
                _mediaRecorder?.Reset();
                System.Diagnostics.Debug.WriteLine($"Audio saved to: {_audioFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MediaRecorder stop error: {ex.Message}");
            }
        }

        private void StopSpeechRecognition()
        {
            try
            {
                _speechRecognizer?.StopListening();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SpeechRecognizer stop error: {ex.Message}");
            }
        }

        // IRecognitionListener Implementation
        

        public void OnPartialResults(Bundle? partialResults)
        {
            var matches = partialResults?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches != null && matches.Count > 0)
            {
                TranscriptionReceived?.Invoke(this, matches[0]);
            }
        }        

        private async void TranscribeBackupAudio()
        {
            if (File.Exists(_audioFilePath))
            {
                try
                {
                    // Hier mit externem Service transkribieren (Azure, Whisper, etc.)
                    var backupTranscription = await TranscribeWithExternalService(_audioFilePath);
                    if (!string.IsNullOrEmpty(backupTranscription))
                    {
                        CompleteTranscriptionReceived?.Invoke(this, backupTranscription);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Backup transcription error: {ex.Message}");
                }
            }
        }

        private async Task<string?> TranscribeWithExternalService(string audioPath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Starting external transcription for: {audioPath}");

                // Versuche zuerst mit Android SpeechRecognizer
                var result = await TranscribeWithAndroidSpeech(audioPath);
                if (!string.IsNullOrEmpty(result))
                {
                    System.Diagnostics.Debug.WriteLine("Android SpeechRecognizer transcription successful");
                    return result;
                }

                return null;

                // Fallback: Azure, Whisper, etc.
                //System.Diagnostics.Debug.WriteLine("Trying fallback transcription service...");
                //return await TranscribeWithCloudService(audioPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"External transcription error: {ex.Message}");
                return null;
            }
        }

        private async Task<string> TranscribeWithAndroidSpeech(string audioPath)
        {
            try
            {
                if (_speechRecognizer == null)
                {
                    System.Diagnostics.Debug.WriteLine("SpeechRecognizer not available");
                    return null;
                }

                // TaskCompletionSource für async/await Pattern
                _transcriptionTask = new TaskCompletionSource<string>();

                // Speziellen Intent für Audio-Datei erstellen
                var fileIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                fileIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
                fileIntent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.German);
                fileIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 5);

                // Audio-Datei übergeben (funktioniert nicht direkt, benötigt Workaround)
                // fileIntent.PutExtra(RecognizerIntent.ExtraSpeechInput, audioPath);

                // Workaround: Audio-Datei in Byte-Array lesen und verarbeiten
                var audioBytes = File.ReadAllBytes(audioPath);
                return await ProcessAudioWithSpeechRecognizer(audioBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Android speech transcription error: {ex.Message}");
                _transcriptionTask?.TrySetResult(null);
                return null;
            }
        }

        private async Task<string> ProcessAudioWithSpeechRecognizer(byte[] audioBytes)
        {
            try
            {
                // Hier müsste die Audio-Datei in ein Format konvertiert werden,
                // das der SpeechRecognizer verarbeiten kann.
                // Dies ist ein komplexer Prozess und erfordert Audio-Konvertierung.

                // Für einfache Fälle: Verwende den Live-SpeechRecognizer neu
                return await UseLiveSpeechRecognizerForFile();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio processing error: {ex.Message}");
                return null;
            }
        }

        private async Task<string> UseLiveSpeechRecognizerForFile()
        {
            // Alternative: Starte den SpeechRecognizer neu und warte auf Ergebnis
            var completionSource = new TaskCompletionSource<string>();
            var timeoutTask = Task.Delay(10000); // 10 Sekunden Timeout

            EventHandler<string> completeHandler = null;
            completeHandler = (s, transcription) =>
            {
                completionSource.TrySetResult(transcription);
                CompleteTranscriptionReceived -= completeHandler;
            };

            CompleteTranscriptionReceived += completeHandler;

            // SpeechRecognizer starten
            _speechRecognizer.StartListening(_speechIntent);

            // Auf Ergebnis oder Timeout warten
            var completedTask = await Task.WhenAny(completionSource.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                CompleteTranscriptionReceived -= completeHandler;
                _speechRecognizer.StopListening();
                return null;
            }

            return await completionSource.Task;
        }

        // Vereinfachte Alternative: Direkte Audio-Analyse
        private async Task<string> TranscribeWithAndroidSpeechSimplified(string audioPath)
        {
            try
            {
                // Starte SpeechRecognizer und verwende ihn direkt
                if (_speechRecognizer != null)
                {
                    var result = await StartSpeechRecognitionWithTimeout();
                    if (!string.IsNullOrEmpty(result))
                    {
                        return result;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Simplified Android speech error: {ex.Message}");
                return null;
            }
        }

        private async Task<string> StartSpeechRecognitionWithTimeout()
        {
            var tcs = new TaskCompletionSource<string>();
            var timeout = Task.Delay(8000); // 8 Sekunden Timeout

            // Temporärer Event Handler
            void OnCompleteHandler(object s, string transcription)
            {
                if (!string.IsNullOrEmpty(transcription))
                {
                    tcs.TrySetResult(transcription);
                }
            }

            void OnErrorHandler(object s, SpeechRecognizerError error)
            {
                tcs.TrySetResult(null);
            }

            CompleteTranscriptionReceived += OnCompleteHandler;

            try
            {
                _speechRecognizer.StartListening(_speechIntent);

                var completedTask = await Task.WhenAny(tcs.Task, timeout);

                if (completedTask == timeout)
                {
                    _speechRecognizer.StopListening();
                    return null;
                }

                return await tcs.Task;
            }
            finally
            {
                CompleteTranscriptionReceived -= OnCompleteHandler;
            }
        }

        private async Task<string> TranscribeWithCloudService(string audioPath)
        {
            // Implementierung für Cloud-Services (Azure, Whisper, etc.)
            try
            {
                // Azure Speech Services Beispiel
                // return await TranscribeWithAzure(audioPath);

                // OpenAI Whisper Beispiel
                // return await TranscribeWithWhisper(audioPath);

                // Google Cloud Speech Beispiel
                // return await TranscribeWithGoogleCloud(audioPath);

                // Für jetzt: Placeholder
                System.Diagnostics.Debug.WriteLine("Cloud service transcription not implemented");
                return "Transkription über Cloud-Service (nicht implementiert)";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cloud service error: {ex.Message}");
                return null;
            }
        }

        // IRecognitionListener Implementation
        // IRecognitionListener Implementation
        public void OnResults(Bundle? results)
        {
            var matches = results?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches != null && matches.Count > 0)
            {
                var transcription = matches[0];
                if (!string.IsNullOrEmpty(transcription))
                {
                    CompleteTranscriptionReceived?.Invoke(this, transcription);
                    _transcriptionTask?.TrySetResult(transcription);
                    System.Diagnostics.Debug.WriteLine($"SpeechRecognizer result: {transcription}");
                }
            }
            else
            {
                // Fallback zur Audio-Datei-Transkription
                _transcriptionTask?.TrySetResult(null);
                Task.Run(async () =>
                {
                    var backupResult = await TranscribeWithExternalService(_audioFilePath);
                    if (!string.IsNullOrEmpty(backupResult))
                    {
                        CompleteTranscriptionReceived?.Invoke(this, backupResult);
                    }
                });
            }
        }

        public void OnError([GeneratedEnum] SpeechRecognizerError error)
        {
            System.Diagnostics.Debug.WriteLine($"SpeechRecognizer error: {error}");
            _transcriptionTask?.TrySetResult(null);

            // Bei Fehler: Backup-Audio verwenden
            if (_isRecording)
            {
                Task.Run(async () =>
                {
                    var backupResult = await TranscribeWithExternalService(_audioFilePath);
                    if (!string.IsNullOrEmpty(backupResult))
                    {
                        CompleteTranscriptionReceived?.Invoke(this, backupResult);
                    }
                });
            }
        }

        private async Task<string> StartAndWaitForRecognition()
        {
            var tcs = new TaskCompletionSource<string>();
            var timeout = Task.Delay(5000); // 5 Sekunden Timeout

            // Temporärer Event Handler
            EventHandler<string> handler = null;
            handler = (s, text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
                    tcs.TrySetResult(text);
                }
            };

            CompleteTranscriptionReceived += handler;

            try
            {
                _speechRecognizer.StartListening(_speechIntent);

                var completedTask = await Task.WhenAny(tcs.Task, timeout);

                if (completedTask == timeout)
                {
                    _speechRecognizer.StopListening();
                    return "Timeout - no speech detected";
                }

                return await tcs.Task;
            }
            finally
            {
                CompleteTranscriptionReceived -= handler;
            }
        }

        // Weitere IRecognitionListener Methoden...
        public void OnReadyForSpeech(Bundle? @params) { }
        public void OnBeginningOfSpeech() { }
        public void OnRmsChanged(float rmsdB) { }
        public void OnBufferReceived(byte[]? buffer) { }
        public void OnEndOfSpeech() { }
        public void OnEvent(int eventType, Bundle? @params) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mediaRecorder?.Release();
                _speechRecognizer?.Destroy();
            }
            base.Dispose(disposing);
        }
    }
}
