using Android.Speech;
using IntelliJ.Lang.Annotations;
using Newtonsoft.Json;
using RepeatList.Models;
using RepeatList.Platforms.Android.Services;
using RepeatList.ViewModels;
using System.Text;
using System.Text.Json;

namespace RepeatList.Services
{
    class DeepSeekClient
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        PositionsPageViewModel _positionsPageViewModel;
        private readonly IAudioTranscriber _transcriber;
        private string _transcribedText;

        public DeepSeekClient(string apiKey = "sk-a3240964efda4aa1aa6cf6ffcf9713b2")
        {
            _apiKey = apiKey;

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(3) };
            _httpClient.BaseAddress = new Uri("https://api.deepseek.com/v1/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            _transcriber = new AndroidTranscriber();
            _positionsPageViewModel = new PositionsPageViewModel(_transcriber);

            //_transcriber = DependencyService.Get<IAudioTranscriber>();
            _transcriber.TranscriptionReceived += OnTranscriptionReceived;
        }

        private void OnTranscriptionReceived(object sender, string text)
        {
            _transcribedText = text;
        }

        public async Task<CompletionResult> GetCompletionAsync(string prompt, string type = "application/json")
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
                type
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
            cost = cost * 1.7m;


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
        public async Task<string> TranscribeToShoppingList(string transcription)  //string audioFilePath)
        {
            try
            {
                // 1. Audio zu Text transkribieren
                //var transcription = await TranscribeWithWhisperNet(audioFilePath);

                // 2. Text zu Einkaufsliste verarbeiten
                if (!string.IsNullOrEmpty(transcription))
                    return await CreateShoppingList(transcription);

                return string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception($"API Fehler: {ex.Message}");
            }
        }

        public async Task<string> TranscribeWithWhisperNet(string audioFile, string language = "de")
        {
            // Verwende die sichere Audio-Vorbereitung
            string processedAudioFile = await PrepareAudioSafeAsync(audioFile);

            try
            {
                var sb = new StringBuilder();
                string modelFilePath;

                if (string.IsNullOrEmpty(processedAudioFile))
                    throw new Exception("Failed to prepare audio file for transcription.");

                // Verwende OpenAI Whisper API für die Transkription
                //var transcriptionText = await TranscribeAudioWithWhisper(processedAudioFile);

                var speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(Platform.AppContext);

                var hasPermission = await _transcriber.RequestPermissionsAsync();
                if (hasPermission)
                {
                    await _transcriber.StartRecordingAsync();
                }

                //
                // Whisper.net  Code auskommentiert, aber eigentlich funktioniert es auch so.
                //
                //#if ANDROID
                //                modelFilePath = await CopyModelToCacheAsync(m_c_model_file_name);
                //#else
                //        modelFilePath = Path.Combine(FileSystem.AppDataDirectory, m_c_model_file_name);
                //#endif

                //                using var whisperFactory = WhisperFactory.FromPath(modelFilePath);
                //                using var processor = whisperFactory
                //                    .CreateBuilder()
                //                    .WithLanguage(language)
                //                    .Build();

                //                using var fileStream = File.OpenRead(processedAudioFile);

                //                await foreach (var segment in processor.ProcessAsync(fileStream))
                //                {
                //                    if (!string.IsNullOrWhiteSpace(segment.Text))
                //                        sb.AppendLine(segment.Text.Trim());
                //                }

                return sb.ToString().Trim();
            }
            finally
            {
                // Aufräumen
                if (processedAudioFile != audioFile && File.Exists(processedAudioFile))
                {
                    try { File.Delete(processedAudioFile); } catch { }
                }
            }
        }

#if ANDROID
        private async Task<string> CopyModelToCacheAsync(string modelFileName)
        {
            var tempPath = Path.Combine(FileSystem.CacheDirectory, modelFileName);

            if (!File.Exists(tempPath))
            {
                using var assetStream = Android.App.Application.Context.Assets.Open(modelFileName);
                using var fileStream = File.Create(tempPath);
                await assetStream.CopyToAsync(fileStream);
            }

            return tempPath;
        }
#endif

        private async Task<string> PrepareAudioSafeAsync(string audioFilePath)
        {
            if (!File.Exists(audioFilePath))
                throw new FileNotFoundException($"Audio file not found: {audioFilePath}");

            // Prüfe ob es bereits eine gültige WAV-Datei ist
            if (await IsValidWhisperAudioAsync(audioFilePath))
                return audioFilePath;

            // Versuche verschiedene Konvertierungsmethoden
            return await ConvertAudioMultipleMethodsAsync(audioFilePath);
        }

        private async Task<string> ConvertAudioMultipleMethodsAsync(string inputFilePath)
        {
            // Methode 1: Einfache Format-Konvertierung
            try
            {
                return await ConvertUsingBasicWaveFormat(inputFilePath);
            }
            catch (Exception ex1)
            {
                Console.WriteLine($"Method 1 failed: {ex1.Message}");
            }

            // Methode 2: Block-based Resampling
            try
            {
                return await ConvertUsingBlockResampling(inputFilePath);
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"Method 2 failed: {ex2.Message}");
            }

            // Methode 3: Verwende originale Datei (letzter Ausweg)
            Console.WriteLine("Using original file as fallback - may not work");
            return inputFilePath;
        }

        private async Task<string> ConvertUsingBasicWaveFormat(string inputFilePath)
        {
            string outputFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".wav");

            using var reader = new NAudio.Wave.AudioFileReader(inputFilePath);

            // Konvertiere zu 16-bit PCM ohne Sample-Rate Änderung
            var outputFormat = new NAudio.Wave.WaveFormat(reader.WaveFormat.SampleRate, 16, 1);

            using var conversionStream = new NAudio.Wave.WaveFormatConversionStream(outputFormat, reader);
            NAudio.Wave.WaveFileWriter.CreateWaveFile(outputFilePath, conversionStream);

            return outputFilePath;
        }

        private async Task<string> ConvertUsingBlockResampling(string inputFilePath)
        {
            string outputFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".wav");

            using var reader = new NAudio.Wave.AudioFileReader(inputFilePath);

            // Resampling in kleinen Blöcken
            var targetFormat = new NAudio.Wave.WaveFormat(16000, 16, 1);
            using var writer = new NAudio.Wave.WaveFileWriter(outputFilePath, targetFormat);

            byte[] buffer = new byte[reader.WaveFormat.AverageBytesPerSecond / 10]; // 100ms Blöcke
            int bytesRead;

            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                writer.Write(buffer, 0, bytesRead);
            }

            return outputFilePath;
        }

        private async Task<bool> IsValidWhisperAudioAsync(string filePath)
        {
            try
            {
                using var reader = new NAudio.Wave.WaveFileReader(filePath);
                return reader.WaveFormat.Encoding == NAudio.Wave.WaveFormatEncoding.Pcm &&
                       reader.WaveFormat.BitsPerSample == 16;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> TranscribeAudioWithWhisper(string filePath)
        {
            using var httpClient = new HttpClient();
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("whisper-1"), "model");
            form.Add(new StreamContent(File.OpenRead(filePath)), "file", Path.GetFileName(filePath));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = form;

            var response = await httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(json);
            return result.text;
        }


        // Response-Klassen
        public class DeepSeekResponse
        {
            public string id { get; set; }
            public string @object { get; set; }
            public int created { get; set; }
            public string model { get; set; }
            public List<Choice> choices { get; set; }
            public Usage usage { get; set; }  // Das fehlt!
            public string system_fingerprint { get; set; }
        }

        public class Choice
        {
            public int index { get; set; }
            public Message message { get; set; }
            public object logprobs { get; set; }
            public string finish_reason { get; set; }
        }

        public class Message
        {
            public string role { get; set; }
            public string content { get; set; }
        }

        public class PromptTokensDetails
        {
            public int cached_tokens { get; set; }
        }

        public class Usage
        {
            public int prompt_tokens { get; set; }
            public int completion_tokens { get; set; }
            public int total_tokens { get; set; }
            //public PromptCacheUsage prompt_cache_hit_tokens { get; set; }
            //public PromptCacheUsage prompt_cache_miss_tokens { get; set; }
            public int prompt_cache_hit_tokens { get; set; }
            public int prompt_cache_miss_tokens { get; set; }
            public PromptTokensDetails prompt_tokens_details { get; set; }
        }

        public class PromptCacheUsage
        {
            public int input_tokens { get; set; }
            public int output_tokens { get; set; }
        }

        private async Task<string?> CreateShoppingList(string text)
        {
            string language = "English";
            switch (Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName)
            {
                case "en": language = "English"; break;
                case "es": language = "Spanish"; break;
                case "fr": language = "French"; break;
                case "de": language = "German"; break;
                case "it": language = "Italian"; break;
                case "uk": language = "Ukrainian"; break;
                case "ru": language = "Russian"; break;
            }

            // DeepSeek Chat API für Listen-Erstellung
            var prompt = $"Create a list from the following text in the format “Item1 Quantity1;Item2 Quantity2;Item3 Quantity3” if it is a shopping list, " +
                $"for example. Otherwise, create a simple list in the format “Item1;Item2;Item3;”. The list must be created in {language}. " +
                $"The text looks like this: “{text}”. Repetitions are not allowed.";

            var response = await GetCompletionAsync(prompt);

            if (response == null || string.IsNullOrEmpty(response.Content))
            {
                if (Shell.Current  != null)
                    await Shell.Current.DisplayAlert("Error", "No response from API.", "OK");
                return null;
            }

            return response.Content;

            //var json=ForForOnDeepSeekClicked(Mode, json, response);

            //var request = new
            //{
            //    model = "deepseek-chat",
            //    messages = new[] { new { role = "user", content = prompt } },
            //    max_tokens = 1000
            //};

            //var response = await _httpClient.PostAsJsonAsync("https://api.deepseek.com/chat/completions", request);
            //var content = await response.Content.ReadAsStringAsync();



            //var apiResponse = JsonConvert.DeserializeObject<DeepSeekResponse>(content);
            //return apiResponse?.choices?.FirstOrDefault()?.message?.content?.Trim() ?? "No list found";
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
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
