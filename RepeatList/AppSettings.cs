using System.Reflection;
using System.Text.Json;

namespace RepeatList
{

    public class AppSettings
    {
        //public DatabaseSettings Database { get; set; }
        //public ApiKeys APIKeys { get; set; }

        public async static Task<AppSettings> Load()
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            //var assembly = Assembly.GetExecutingAssembly();
            //using var _stream = assembly.GetManifestResourceStream(resourceName);

            //var assembly = Assembly.GetExecutingAssembly();
            //var resourceName = "RepeatList.appsettings.json"; // Anpassen!

            //using var stream = assembly.GetManifestResourceStream(resourceName);
            //using var reader = new StreamReader(stream);
            //var json = reader.ReadToEnd();
            var ret_val = JsonSerializer.Deserialize<AppSettings>(json);
            return ret_val;
        }

        //public ConnectionStrings ConnectionStrings { get; set; }
        public ApiKeys ApiKeys { get; set; }
    }

    public class ApiKeys
    {
        public string SupabaseKey { get; set; }
    }




}
