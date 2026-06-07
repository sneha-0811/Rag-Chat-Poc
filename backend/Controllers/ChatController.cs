using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly EmbeddingService _embedding;
    private readonly QdrantService _qdrant;
    private readonly ChatService _chat;

    public ChatController(EmbeddingService embedding, QdrantService qdrant, ChatService chat)
    {
        _embedding = embedding;
        _qdrant = qdrant;
        _chat = chat;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question cannot be empty.");

        var queryVector = await _embedding.EmbedAsync(request.Question);
        var context = await _qdrant.SearchAsync(queryVector, topK: 3);
        var answer = await _chat.AskAsync(request.Question, context);

        return Ok(new { answer });
    }
}

public record ChatRequest(string Question);
