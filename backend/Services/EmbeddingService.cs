using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace backend.Services;

public class EmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public EmbeddingService(IConfiguration config, HttpClient http)
    {
        _http = http;
        _apiKey = config["Cohere:ApiKey"]!;
        _model = config["Cohere:EmbeddingModel"]!;
    }

    public async Task<float[]> EmbedAsync(string text)
    {
        var url = "https://api.cohere.ai/v1/embed";

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var body = new
        {
            texts = new[] { text },
            model = _model,
            input_type = "search_document"
        };

        var response = await _http.PostAsync(url,
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        // Cohere returns { "embeddings": [[...values...]] }
        return doc.RootElement
            .GetProperty("embeddings")[0]
            .EnumerateArray()
            .Select(v => v.GetSingle())
            .ToArray();
    }
}
