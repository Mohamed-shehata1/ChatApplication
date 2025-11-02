using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MyChatApp.Models;

namespace MyChatApp.Services
{
    public class OpenAiService
    {
        private readonly HttpClient _http;
        private readonly OpenAiOptions _options;
        private readonly ILogger<OpenAiService> _logger;

        public OpenAiService(HttpClient http, IOptions<OpenAiOptions> options, ILogger<OpenAiService> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> SendChatAsync(IEnumerable<Message> history, string model)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("OpenAI API key is missing in configuration.");
                return "ERROR: Missing OpenAI API key in configuration.";
            }

            // Build correct URL ensuring base ends with a slash
            var baseUrl = (_options.BaseUrl ?? "https://api.openai.com/v1").TrimEnd('/');
            var url = new Uri($"{baseUrl}/chat/completions");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

                var messages = history.Select(m => new { role = m.Role, content = m.Content }).ToList();

                var payload = new
                {
                    model = model ?? _options.DefaultModel,
                    messages = messages
                };

                var payloadJson = JsonSerializer.Serialize(payload);
                _logger.LogDebug("OpenAI request payload: {Payload}", payloadJson);

                request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                var resp = await _http.SendAsync(request);

                var json = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    var err = $"HTTP {(int)resp.StatusCode} - {resp.ReasonPhrase}: {json}";
                    _logger.LogWarning("OpenAI request failed: {Error}", err);
                    // Return an error string prefixed so caller can detect
                    return $"ERROR: {err}";
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                // Try to extract choices[0].message.content
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var first = choices[0];
                    if (first.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
                    {
                        var result = content.GetString() ?? string.Empty;
                        _logger.LogDebug("OpenAI reply: {Reply}", result);
                        return result;
                    }
                }

                _logger.LogDebug("OpenAI raw response: {Response}", json);
                // Fallback: return entire response
                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling OpenAI");
                return $"ERROR: Exception - {ex.Message}";
            }
        }
    }
}
