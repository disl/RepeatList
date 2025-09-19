using Android.Media;
using Application = Android.App.Application;

namespace RepeatList.Platforms.Android.Services
{
    public static class MediaScannerService
    {
        public static void ScanFile(string filePath)
        {
            try
            {
                var connection = new MediaScannerConnection(
                    Application.Context, // <--- Korrigiert: Verwende Application.Context direkt
                    null // Platzhalter, wird gleich gesetzt
                );
                var client = new MediaScannerClient(filePath, connection);
                // Setze den Client im Connection-Objekt
                typeof(MediaScannerConnection)
                    .GetField("mClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(connection, client);

                connection.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MediaScanner error: {ex.Message}");
            }
        }

        private class MediaScannerClient : Java.Lang.Object, MediaScannerConnection.IMediaScannerConnectionClient
        {
            private readonly string _filePath;
            private readonly MediaScannerConnection _connection;

            public MediaScannerClient(string filePath, MediaScannerConnection connection)
            {
                _filePath = filePath;
                _connection = connection;
            }

            public void OnMediaScannerConnected()
            {
                _connection.ScanFile(_filePath, null);
            }

            public void OnScanCompleted(string? path, global::Android.Net.Uri? uri)
            {
                Console.WriteLine($"File scanned: {path}");
                _connection.Disconnect();
            }
        }
    }
}
