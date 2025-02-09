using CommunityToolkit.Maui.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepeatList.Services
{
    public class SpeechService
    {
        public async Task<string> StartSpeechToTextAsync()
        {
            var cancellationToken = new CancellationTokenSource().Token;

            // Berechtigungen anfordern
            var isGranted = await SpeechToText.Default.RequestPermissions(cancellationToken);
            if (!isGranted)
            {
                return "Berechtigungen nicht erteilt.";
            }

            // Spracherkennung starten
            var recognitionResult = await SpeechToText.Default.ListenAsync(
                new CultureInfo("de-DE"), // Deutsche Sprache
                new Progress<string>(partialText =>
                {
                    Console.WriteLine($"Zwischenergebnis: {partialText}");
                }),
                cancellationToken);

            return recognitionResult.IsSuccessful ? recognitionResult.Text : "Erkennung fehlgeschlagen.";
        }
    }
}
