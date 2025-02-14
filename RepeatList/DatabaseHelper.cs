using System.Reflection;

namespace RepeatList
{

    public static class DatabaseHelper
    {
        public static async Task CopyDatabaseToAppData(string databaseName)
        {
            // Pfad zur lokalen Datenbankdatei
            var localDbPath = Path.Combine(FileSystem.AppDataDirectory, databaseName);

            // Überprüfen, ob die Datenbankdatei bereits existiert
            if (!File.Exists(localDbPath))
            {
                // Datenbankdatei aus eingebetteten Ressourcen laden
                var assembly = IntrospectionExtensions.GetTypeInfo(typeof(App)).Assembly;
                var resourceName = $"RepeatList.Resources.{databaseName}";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new FileNotFoundException($"Database resource '{resourceName}' not found.");
                    }

                    // Datenbankdatei in das lokale Dateisystem kopieren
                    using (var fileStream = new FileStream(localDbPath, FileMode.Create, FileAccess.Write))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }
            }
        }
    }
}
