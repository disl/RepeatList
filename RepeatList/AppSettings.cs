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
            var ret_val = JsonSerializer.Deserialize<AppSettings>(json);
            return ret_val;
        }

        //public async static Task<SpotifyInfo> LoadSpotifyInfo()
        //{
        //    using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
        //    using var reader = new StreamReader(stream);
        //    var json = reader.ReadToEnd();
        //    var ret_val = JsonSerializer.Deserialize<SpotifyInfo>(json);
        //    return ret_val;
        //}

        //public ConnectionStrings ConnectionStrings { get; set; }
        public ApiKeys ApiKeys { get; set; }

        public SpotifyInfo SpotifyInfo { get; set; }
    }

    public class ApiKeys
    {
        public string SupabaseKey { get; set; }
    }

    public class  SpotifyInfo
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public string RedirectUri { get; set; }

        /*  
         "SpotifyInfo": {
    "ClientId": "5ab07e8eb1c84d3486dccd60767bb282",
    "ClientSecret": "4741c5297cc94ad6ba4463857fd76552",
    "RedirectUri"
         */
    }




}
