namespace RepeatList.Models
{
    public interface IAudioTranscriber
    {
        Task<bool> RequestPermissionsAsync();
        Task<string> StartRecordingAsync();
        void StopRecording();
        bool IsRecording { get; }

        // Für Live-Updates während der Aufnahme
        event EventHandler<string> TranscriptionReceived;

        // Für das finale Ergebnis nach StopRecording
        event EventHandler<string> CompleteTranscriptionReceived;
    }

 
}