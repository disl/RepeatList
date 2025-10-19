using CommunityToolkit.Maui.Media;
using System.Globalization;

namespace RepeatList.Services
{
    public class SpeechToTextService
    {
        public async Task<string> StartListeningAsync()
        {
            try
            {
                // 1. Berechtigungen prüfen
                var permissionStatus = await Permissions.RequestAsync<Permissions.Microphone>();
                if (permissionStatus != PermissionStatus.Granted)
                {
                    return "Mikrofon-Berechtigung verweigert";
                }

                // 2. Speech Recognition mit der KORREKTEN Methode
                var recognitionResult = await SpeechToText.Default.RecognizeSpeechAsync(
                    new SpeechToTextOptions()
                    {
                        Culture = "de-DE", // Deutsch
                        MaximumRecordingLength = TimeSpan.FromSeconds(30)
                    });

                // 3. Ergebnis verarbeiten
                if (recognitionResult.IsSuccessful)
                {
                    return recognitionResult.Text ?? "Keine Sprache erkannt";
                }
                else
                {
                    return $"Fehler: {recognitionResult.Exception?.Message}";
                }
            }
            catch (Exception ex)
            {
                return $"Ausnahme: {ex.Message}";
            }
        }
    }
}