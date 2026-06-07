using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace backend.Services;

public class ChatService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public ChatService(IConfiguration config, HttpClient http)
    {
        _http = http;
        _apiKey = config["Groq:ApiKey"]!;
        _model = config["Groq:ChatModel"]!;
    }

    public async Task<string> AskAsync(string question, List<(string Text, string Source)> context)
    {
        var contextText = string.Join("\n\n", context.Select(c => $"[{c.Source}]\n{c.Text}"));

        var prompt = $"""
            Answer the following question using only the context provided below.
            If the answer is not in the context, say "I don't have information about that."

            Context:
            {contextText}

            Question: {question}
            """;

        var url = "https://api.groq.com/openai/v1/chat/completions";

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var body = new
        {
            model = _model,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var response = await _http.PostAsync(url,
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "No response.";
    }
}
