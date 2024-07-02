using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeminiChessAnalysis.Services
{
    public class GeminiApiClient
    {
        private static GeminiApiClient _instance;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey= "YOUR_API_KEY";
        private readonly string _apiUrl;

        public GeminiApiClient()
        {
            _httpClient = new HttpClient();
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("API key not found. Please set the YOUR_API_KEY environment variable.");
            }
            _apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key=" + _apiKey;
        }

        public static GeminiApiClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GeminiApiClient();
                }
                return _instance;
            }
        }

        public async Task<string> GenerateContentAsync(string text)
        {
            var requestContent = new
            {
                contents = new[]
                {
                new
                {
                    parts = new[]
                    {
                        new { text }
                    }
                }
            }
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_apiUrl, jsonContent);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return ParseStoryFromResponse(responseContent);
        }

        private string ParseStoryFromResponse(string jsonResponse)
        {
            try
            {
                var jObject = JObject.Parse(jsonResponse);
                var storyText = jObject["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                return storyText ?? "No story found.";
            }
            catch (Exception ex)
            {
                return $"Failed to parse response: {ex.Message}";
            }
        }
    }
}
