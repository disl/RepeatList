using Newtonsoft.Json;
using RepeatList.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RepeatList.Services
{
    class DeepSeekClient
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private const string DailyQueryCountKey = "QueriesToday";

        public DeepSeekClient(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.deepseek.com/v1/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<CompletionResult> GetCompletionAsync(string prompt)
        {
            var requestBody = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("chat/completions", jsonContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"DeepSeek API error: {response.StatusCode} - {responseContent}");
            }

            // Deserialisieren
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var responseData = System.Text.Json.JsonSerializer.Deserialize<DeepSeekResponse>(responseContent, options);

            string content = responseData?.choices?[0]?.message?.content?.Trim() ?? "";
            int promptTokens = responseData?.usage?.prompt_tokens ?? 0;
            int completionTokens = responseData?.usage?.completion_tokens ?? 0;

            // Kosten berechnen
            decimal cost = (promptTokens * 0.0015m / 1000) + (completionTokens * 0.0020m / 1000);

            // Korrigieren der Kostenberechnung
            cost = cost * 2;



            // TEST !!!!!!!!
            //cost=1.9863450m;


            var DailyQueryCount = Preferences.Get("QueriesToday", 0);

            if (!DeepSeekBilling.DeductFromUserCredit(cost) && DailyQueryCount > 3)
            {
                throw new CreditIsInsufficientError(777, Properties.Resources.insufficient_credit);
            }

            return new CompletionResult
            {
                Content = content,
                Cost = cost
            };
        }


        // Audio recognation methods
        public async Task<string> TranscribeToShoppingList(string audioFilePath)
        {
            try
            {
                // 1. Audio zu Text transkribieren
                var transcription = await TranscribeAudio(audioFilePath);

                // 2. Text zu Einkaufsliste verarbeiten
                return await CreateShoppingList(transcription);
            }
            catch (Exception ex)
            {
                throw new Exception($"DeepSeek API Fehler: {ex.Message}");
            }
        }

        private async Task<string> TranscribeAudio(string filePath)
        {
            // DeepSeek Audio API Aufruf
            var audioBytes = await File.ReadAllBytesAsync(filePath);
            var base64Audio = Convert.ToBase64String(audioBytes);

            var request = new
            {
                audio = base64Audio,
                model = "deepseek-audio",
                response_format = "text"
            };

            var response = await _httpClient.PostAsJsonAsync("https://api.deepseek.com/chat/completions", request);
            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TranscriptionResponse>(content)?.text ?? "";
        }

        private async Task<string> CreateShoppingList(string text)
        {
            // DeepSeek Chat API für Listen-Erstellung
            var prompt = $"Erstelle aus folgendem Text eine Einkaufsliste im Format 'item1; item2; item3'. Nur die Liste ausgeben, sonst nichts:\n\n{text}";

            var request = new
            {
                model = "deepseek-chat",
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 500
            };

            var response = await _httpClient.PostAsJsonAsync("https://api.deepseek.com/chat/completions", request);
            var content = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonConvert.DeserializeObject<DeepSeekResponse>(content);
            return apiResponse?.choices?.FirstOrDefault()?.message?.content?.Trim() ?? "Keine Liste erkannt";
        }

    }

    // Response-Klassen
    public class TranscriptionResponse { public string text { get; set; } }
    //public class DeepSeekResponse { public List<Choice> Choices { get; set; } }
    //public class Choice { public Message Message { get; set; } }
    //public class Message { public string Content { get; set; } }

    public class CompletionResult
    {
        public string Content { get; set; }
        public decimal Cost { get; set; }
    }

    public class DeepSeekResponse
    {
        public List<Choice> choices { get; set; }
        public Usage usage { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }
}
