using RepeatList.Platforms.Android.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepeatList.Services
{
    public interface IFileExportService
    {
        Task<bool> ExportToDownloadsAsync(string filename, string content);
        Task<bool> ExportToDownloadsAsync(string filename, IEnumerable<string> lines);
        Task<string> GetDownloadsPathAsync();
    }

    public class FileExportService : IFileExportService
    {
        public async Task<string> GetDownloadsPathAsync()
        {
            // Für Android
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Primary External Storage (Downloads-Ordner)
                var downloadsPath = Path.Combine(
                    Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath ??
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Android.OS.Environment.DirectoryDownloads);

                return downloadsPath;
            }

            // Für andere Plattformen
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        public async Task<bool> ExportToDownloadsAsync(string filename, string content)
        {
            try
            {
                var downloadsPath = await GetDownloadsPathAsync();
                var filePath = Path.Combine(downloadsPath, filename);

                // Verzeichnis sicherstellen
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // Datei schreiben
                await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);

                // MediaScanner benachrichtigen (nur Android)
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    MediaScannerService.ScanFile(filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export fehlgeschlagen: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExportToDownloadsAsync(string filename, IEnumerable<string> lines)
        {
            var content = string.Join(Environment.NewLine, lines);
            return await ExportToDownloadsAsync(filename, content);
        }
    }
}