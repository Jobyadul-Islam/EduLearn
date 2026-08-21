using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EduLearn.Services
{
    public class GeminiChatService : IChatService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiChatService> _logger;

        public GeminiChatService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GeminiChatService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        private string ApiKey => _configuration["Gemini:ApiKey"] ?? "";
        private string Model => _configuration["Gemini:Model"] ?? "gemini-3.6-flash";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

        public async Task<string> SendMessageAsync(string systemPrompt, List<ChatTurn> history)
        {
            if (!IsConfigured)
            {
                return "The AI assistant isn't fully set up yet — the site owner still needs to add an API key. In the meantime, feel free to browse the Courses page!";
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent";

            var requestBody = new GeminiRequest
            {
                SystemInstruction = new GeminiContent
                {
                    Parts = new List<GeminiPart> { new GeminiPart { Text = systemPrompt } }
                },
                Contents = history.ConvertAll(turn => new GeminiContent
                {
                    Role = turn.Role,
                    Parts = new List<GeminiPart> { new GeminiPart { Text = turn.Text } }
                }),
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = 0.6,
                    MaxOutputTokens = 1024
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", ApiKey);
            request.Content = JsonContent.Create(requestBody, options: JsonOptions);

            try
            {
                var response = await client.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini API error {StatusCode}: {Body}", response.StatusCode, responseText);
                    return "Sorry, I couldn't reach the AI assistant right now. Please try again in a moment.";
                }

                var parsed = JsonSerializer.Deserialize<GeminiResponse>(responseText, JsonOptions);
                var reply = parsed?.Candidates?.Count > 0
                    ? parsed.Candidates[0].Content?.Parts?.Count > 0
                        ? parsed.Candidates[0].Content.Parts[0].Text
                        : null
                    : null;

                return string.IsNullOrWhiteSpace(reply)
                    ? "Sorry, I didn't get a response — could you try rephrasing that?"
                    : reply.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return "Sorry, something went wrong reaching the AI assistant. Please try again shortly.";
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private class GeminiRequest
        {
            [JsonPropertyName("systemInstruction")]
            public GeminiContent SystemInstruction { get; set; }

            [JsonPropertyName("contents")]
            public List<GeminiContent> Contents { get; set; }

            [JsonPropertyName("generationConfig")]
            public GeminiGenerationConfig GenerationConfig { get; set; }
        }

        private class GeminiContent
        {
            [JsonPropertyName("role")]
            public string Role { get; set; }

            [JsonPropertyName("parts")]
            public List<GeminiPart> Parts { get; set; }
        }

        private class GeminiPart
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        private class GeminiGenerationConfig
        {
            [JsonPropertyName("temperature")]
            public double Temperature { get; set; }

            [JsonPropertyName("maxOutputTokens")]
            public int MaxOutputTokens { get; set; }
        }

        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<GeminiCandidate> Candidates { get; set; }
        }

        private class GeminiCandidate
        {
            [JsonPropertyName("content")]
            public GeminiContent Content { get; set; }
        }
    }
}
