using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StockPhotoScout.Interfaces;

namespace StockPhotoScout.Services;

public sealed class OllamaClient : IOllamaClient
{
    private const string OllamaUrl = "http://localhost:11434";
    private const string VisionModel = "qwen2.5vl:7b";

    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OllamaClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri(OllamaUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<bool> IsReadyAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                "/api/tags", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            var result = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(
                JsonOptions, cancellationToken);

            return result?.Models?.Any(model =>
                string.Equals(
                    model.Name,
                    VisionModel,
                    StringComparison.OrdinalIgnoreCase)) == true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    public async Task<string> AnalyzeImageAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentException(
                "Es wurde kein Bildpfad angegeben.", nameof(imagePath));

        if (!File.Exists(imagePath))
            throw new FileNotFoundException(
                "Die Bilddatei wurde nicht gefunden.", imagePath);

        var imageBytes = await File.ReadAllBytesAsync(
            imagePath, cancellationToken);

        var imageBase64 = Convert.ToBase64String(imageBytes);

        var prompt = LoadPrompt();

        var request = new OllamaChatRequest
        {
            Model = VisionModel,
            Stream = false,
            Messages =
            [
                new OllamaChatMessage
                {
                    Role = "user",
                    Content = prompt,
                    Images = [imageBase64]
                }
            ],
            Options = new OllamaOptions
            {
                Temperature = 0.1,
                NumCtx = 8192
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "/api/chat", request, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"Ollama meldet HTTP {(int)response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            JsonOptions, cancellationToken);

        var content = result?.Message?.Content;

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException(
                "Ollama hat keine Analyse zurückgegeben.");

        return content;
    }

    private sealed class OllamaTagsResponse
    {
        public List<OllamaModelInfo>? Models { get; set; }
    }

    private sealed class OllamaModelInfo
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class OllamaChatRequest
    {
        public string Model { get; set; } = string.Empty;
        public bool Stream { get; set; }
        public List<OllamaChatMessage> Messages { get; set; } = [];
        public OllamaOptions Options { get; set; } = new();
    }

    private sealed class OllamaChatMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
        public List<string> Images { get; set; } = [];
    }

    private sealed class OllamaOptions
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("num_ctx")]
        public int NumCtx { get; set; }
    }

    private sealed class OllamaChatResponse
    {
        public OllamaResponseMessage? Message { get; set; }
    }

    private sealed class OllamaResponseMessage
    {
        public string? Content { get; set; }
    }

    private static string LoadPrompt()
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "Prompts",
            "StockPhotoAnalysis.txt");

        if (!File.Exists(promptPath))
        {
            throw new FileNotFoundException(
                "The stock photo analysis prompt was not found.",
                promptPath);
        }

        return File.ReadAllText(promptPath);
    }
}