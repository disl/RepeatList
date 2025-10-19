using Android.Media;

namespace RepeatList.Services
{
    public class AudioRecorderService
    {
        private MediaRecorder _recorder;
        private string _filePath;

        public void StartRecording()
        {
            try
            {
                _filePath = Path.Combine(FileSystem.CacheDirectory, $"audio_{DateTime.Now:yyyyMMdd_HHmmss}.m4a");

                _recorder = new MediaRecorder();
                _recorder.SetAudioSource(AudioSource.Mic);
                _recorder.SetOutputFormat(OutputFormat.Mpeg4);
                _recorder.SetAudioEncoder(AudioEncoder.Aac);
                _recorder.SetAudioSamplingRate(16000); // 16kHz für bessere Sprachqualität
                _recorder.SetAudioChannels(1); // Mono
                _recorder.SetAudioEncodingBitRate(128000); // 128 kbps
                _recorder.SetOutputFile(_filePath);

                _recorder.Prepare();
                _recorder.Start();
            }
            catch (Exception ex)
            {
                throw new Exception($"Aufnahme fehlgeschlagen: {ex.Message}");
            }
        }

        public string StopRecording()
        {
            try
            {
                _recorder?.Stop();
                _recorder?.Reset();
                _recorder?.Release();
                _recorder = null;

                return _filePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Stop fehlgeschlagen: {ex.Message}");
            }
        }
    }
}
