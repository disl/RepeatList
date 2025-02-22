
namespace RepeatList.Services
{
    public static class Utilities
    {
        //public static async Task ShareFileAsync(string FileName, string TextContent, string Title)
        //{
        //    string filePath = Path.Combine(FileSystem.CacheDirectory, FileName);

        //    // Test-Datei erstellen
        //    File.WriteAllText(filePath, TextContent);

        //    var file = new ShareFile(filePath);
        //    var request = new ShareFileRequest
        //    {
        //        Title = Title,
        //        File = file
        //    };

        //    await Share.RequestAsync(request);
        //}

        public static async Task ShareTextAsync(string TextContent)
        {
            await Share.RequestAsync(TextContent);
        }

    }
}
