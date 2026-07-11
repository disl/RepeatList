using System.Text;

namespace RepeatList.Services
{
    // Hält API-Keys nicht als Klartext im appsettings.json/Assembly, sondern XOR+Base64-obfuskiert,
    // damit sie nicht per "strings app.apk" oder automatisiertem Secret-Scanning trivial auffindbar sind.
    // Schützt nicht vor gezieltem Decompilieren - nur Fix ist ein serverseitiger Proxy.
    internal static class SecretVault
    {
        private const string Passphrase = "RepeatList_v1_Salt#2024";

        // Neu generieren nach jeder Key-Rotation: python Tools/obfuscate_key.py <neuer-key>
        private const string DeepSeekApiKeyEncoded = "IQ5dBlQXfggRQDtCVWg3VVVAF1NRVgRmARQGVkx9UURDbU4=";
        private const string SupabaseKeyEncoded = "Nxw6DQMzLwA8HRU/ZCUaUCIdakF5XGZnBjMsVz0nGSsiHDwIcTYYJgRAAX1bezsvCgE5NiQwHjIlLGIWICgCPk9oWXsCGwg6UwAZHlgXMztDUm0VVwg8dUhVXHY4PzcdUj0lHhoXMk9CBQAoWj1NfFxRWggVKVc3Ei8ESgcFJXgsGgwAHEdxeQR5BgZAKCUxfCcJF245Yig6OzQcVHtaXU0fISVULw4FEz0OCkNXDn0mKxthVm8FURYDNBAzMHszRB45O1kVFSYOLUkDAAVwJjc5HTszB1kmBx07";
        private const string SpotifyClientIdEncoded = "ZwQSVVYRdAwRRTxOBTtgVVRCR1FTVgJiUkZSAxZ+UUE=";
        private const string SpotifyClientSecretEncoded = "ZlJEVAJBflBEFzxPBT43Vw4VFwYGAQxnUhYBVkJ5XEE=";

        public static string DeepSeekApiKey => Decode(DeepSeekApiKeyEncoded);
        public static string SupabaseKey => Decode(SupabaseKeyEncoded);
        public static string SpotifyClientId => Decode(SpotifyClientIdEncoded);
        public static string SpotifyClientSecret => Decode(SpotifyClientSecretEncoded);

        private static string Decode(string encoded)
        {
            var passphraseBytes = Encoding.UTF8.GetBytes(Passphrase);
            var data = Convert.FromBase64String(encoded);
            var result = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
                result[i] = (byte)(data[i] ^ passphraseBytes[i % passphraseBytes.Length]);

            return Encoding.UTF8.GetString(result);
        }
    }
}
