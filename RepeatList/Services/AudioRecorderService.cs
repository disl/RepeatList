using Android.Media;

namespace RepeatList.Services
{

    public class AudioRecorderService : IDisposable
    {
        private AudioRecord _audioRecord;
        private bool _isRecording;
        private string _filePath;
        private Thread _recordingThread;

        public void StartRecording()
        {
            try
            {
                //_filePath = Path.Combine(FileSystem.CacheDirectory, $"audio_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

                _filePath = Path.Combine(FileSystem.CacheDirectory, $"audio_output.wav");

                if(File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }

                // Optimierte Audio-Parameter für Sprachaufnahme
                int sampleRate = 16000; // 16kHz für Sprache optimal
                var channelConfig = ChannelIn.Mono;
                var audioFormat = Encoding.Pcm16bit;

                // Puffergröße optimieren
                int minBufferSize = AudioRecord.GetMinBufferSize(sampleRate, channelConfig, audioFormat);
                int bufferSize = Math.Max(minBufferSize * 4, 8192); // Größerer Puffer für Stabilität

                _audioRecord = new AudioRecord(
                    AudioSource.Mic,      // Mikrofonquelle
                    sampleRate,           // Sample-Rate
                    channelConfig,        // Mono
                    audioFormat,          // 16-bit PCM
                    bufferSize);          // Puffergröße

                // Prüfe ob AudioRecord korrekt initialisiert
                if (_audioRecord.State != State.Initialized)
                {
                    throw new Exception("AudioRecord konnte nicht initialisiert werden");
                }

                // Audio-Qualität prüfen
                if (_audioRecord.AudioSource != AudioSource.Mic)
                {
                    Console.WriteLine("Warnung: AudioSource könnte nicht gesetzt werden");
                }

                _isRecording = true;
                _audioRecord.StartRecording();

                // Starte Aufnahme-Thread mit höherer Priorität
                _recordingThread = new Thread(new ThreadStart(RecordToFile))
                {
                    Priority = ThreadPriority.AboveNormal
                };
                _recordingThread.Start();

                Console.WriteLine($"Aufnahme gestartet: {_filePath}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Aufnahme fehlgeschlagen: {ex.Message}");
            }
        }

        public void StopRecording()
        {
            _isRecording = false;
            _audioRecord?.Stop();
            _recordingThread?.Join(1000);
            _audioRecord?.Release();
        }

        private void RecordToFile()
        {
            try
            {
                using var fileStream = new FileStream(_filePath, FileMode.Create);
                WriteWavHeader(fileStream, 16000, 16, 1);

                byte[] buffer = new byte[4096];
                while (_isRecording)
                {
                    int bytesRead = _audioRecord.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                }

                // Update WAV Header mit korrekter Dateigröße
                UpdateWavHeader(fileStream);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Aufnahme-Thread Fehler: {ex.Message}");
            }
        }

        private void WriteWavHeader(FileStream stream, int sampleRate, int bitsPerSample, int channels)
        {
            // WAV Header schreiben
            byte[] header = new byte[44];
            using var writer = new BinaryWriter(new MemoryStream(header));

            // RIFF Header
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(0); // Platzhalter für Dateigröße
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

            // fmt chunk
            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16); // SubChunk1Size
            writer.Write((short)1); // AudioFormat (PCM)
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bitsPerSample / 8); // ByteRate
            writer.Write((short)(channels * bitsPerSample / 8)); // BlockAlign
            writer.Write((short)bitsPerSample);

            // data chunk
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(0); // Platzhalter für Daten-Größe

            stream.Write(header, 0, header.Length);
        }

        private void UpdateWavHeader(FileStream stream)
        {
            if (stream.Length < 44) return;

            stream.Seek(0, SeekOrigin.Begin);

            using var writer = new BinaryWriter(stream);
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write((int)(stream.Length - 8)); // Dateigröße - 8

            writer.Seek(40, SeekOrigin.Begin);
            writer.Write((int)(stream.Length - 44)); // Daten-Größe

            stream.Flush();
        }

        public string GetFilePath() => _filePath;

        public void Dispose()
        {
            _isRecording = false;
            _audioRecord?.Release();
            _audioRecord = null;
        }
    }

    //public class AudioRecorderService
    //{
    //    private MediaRecorder _recorder;
    //    private string _filePath;

    //    public void StartRecording()
    //    {
    //        try
    //        {
    //            _filePath = Path.Combine(FileSystem.CacheDirectory, $"audio_{DateTime.Now:yyyyMMdd_HHmmss}.m4a");

    //            _recorder = new MediaRecorder();
    //            _recorder.SetAudioSource(AudioSource.Mic);
    //            _recorder.SetOutputFormat(OutputFormat.Mpeg4);
    //            _recorder.SetAudioEncoder(AudioEncoder.Aac);
    //            _recorder.SetAudioSamplingRate(16000); // 16kHz für bessere Sprachqualität
    //            _recorder.SetAudioChannels(1); // Mono
    //            _recorder.SetAudioEncodingBitRate(128000); // 128 kbps
    //            _recorder.SetOutputFile(_filePath);

    //            _recorder.Prepare();
    //            _recorder.Start();
    //        }
    //        catch (Exception ex)
    //        {
    //            throw new Exception($"Aufnahme fehlgeschlagen: {ex.Message}");
    //        }
    //    }

    //    public string StopRecording()
    //    {
    //        try
    //        {
    //            _recorder?.Stop();
    //            _recorder?.Reset();
    //            _recorder?.Release();
    //            _recorder = null;

    //            return _filePath;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw new Exception($"Stop fehlgeschlagen: {ex.Message}");
    //        }
    //    }
    //}
}
