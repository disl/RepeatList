using Android.Media;

namespace RepeatList.Services
{
    public class AudioRecorderService
    {
        private MediaRecorder _recorder;
        private string _filePath;

        public void StartRecording()
        {
            _filePath = Path.Combine(FileSystem.CacheDirectory, $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

            _recorder = new MediaRecorder();
            _recorder.SetAudioSource(AudioSource.Mic);
            _recorder.SetOutputFormat(OutputFormat.Mpeg4);
            _recorder.SetAudioEncoder(AudioEncoder.Aac);
            _recorder.SetAudioSamplingRate(16000);
            _recorder.SetAudioChannels(1);
            _recorder.SetOutputFile(_filePath);

            _recorder.Prepare();
            _recorder.Start();
        }

        public string StopRecording()
        {
            _recorder?.Stop();
            _recorder?.Release();
            _recorder = null;
            return _filePath;
        }
    }
}
