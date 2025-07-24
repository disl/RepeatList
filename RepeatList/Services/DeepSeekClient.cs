using RepeatList.Models;
using System.Text;
using System.Text.Json;

namespace RepeatList.Services
{
    class DeepSeekClient
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

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
                JsonSerializer.Serialize(requestBody),
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
            var responseData = JsonSerializer.Deserialize<DeepSeekResponse>(responseContent, options);

            string content = responseData?.choices?[0]?.message?.content?.Trim() ?? "";
            int promptTokens = responseData?.usage?.prompt_tokens ?? 0;
            int completionTokens = responseData?.usage?.completion_tokens ?? 0;

            // Kosten berechnen
            decimal cost = (promptTokens * 0.0015m / 1000) + (completionTokens * 0.0020m / 1000);

            // Korrigieren der Kostenberechnung
            cost = cost * 2;



            // TEST
            //cost=1.9863450m;



            if (!DeepSeekBilling.DeductFromUserCredit(cost))
            {
                throw new CreditIsInsufficientError(777, Properties.Resources.insufficient_credit);
            }

            return new CompletionResult
            {
                Content = content,
                Cost = cost
            };
        }



        //public async Task<string> GetCompletion(string prompt)
        //{
        //    var requestBody = new
        //    {
        //        model = "deepseek-chat",
        //        messages = new[]
        //        {
        //        new { role = "user", content = prompt }
        //    }
        //    };

        //    var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
        //    var content = new StringContent(json, Encoding.UTF8, "application/json");

        //    var response = await _httpClient.PostAsync("chat/completions", content);
        //    response.EnsureSuccessStatusCode();

        //    var responseJson = await response.Content.ReadAsStringAsync();
        //    dynamic responseData = Newtonsoft.Json.JsonConvert.DeserializeObject(responseJson);

        //    return responseData.choices[0].message.content;
        //}
    }

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
