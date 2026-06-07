using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestController : ControllerBase
{
    private readonly EmbeddingService _embedding;
    private readonly ChunkingService _chunking;
    private readonly QdrantService _qdrant;
    private readonly string _docsPath;

    public IngestController(EmbeddingService embedding, ChunkingService chunking, QdrantService qdrant, IConfiguration config)
    {
        _embedding = embedding;
        _chunking = chunking;
        _qdrant = qdrant;
        _docsPath = Path.GetFullPath(config["DocsPath"]!);
    }

    [HttpPost]
    public async Task<IActionResult> Ingest()
    {
        await _qdrant.EnsureCollectionAsync();

        var files = Directory.GetFiles(_docsPath, "*.txt");
        if (files.Length == 0)
            return BadRequest("No .txt files found in docs folder.");

        ulong id = 0;
        foreach (var file in files)
        {
            var text = await System.IO.File.ReadAllTextAsync(file);
            var source = Path.GetFileName(file);
            var chunks = _chunking.Chunk(text);

            foreach (var chunk in chunks)
            {
                var vector = await _embedding.EmbedAsync(chunk);
                await _qdrant.UpsertAsync(id++, vector, chunk, source);
            }
        }

        return Ok(new { message = $"Ingested {id} chunks from {files.Length} files." });
    }
}
