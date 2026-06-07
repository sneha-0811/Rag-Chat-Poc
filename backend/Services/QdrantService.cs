using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace backend.Services;

public class QdrantService
{
    private readonly QdrantClient _client;
    private readonly string _collection;
    private const ulong VectorSize = 384;

    public QdrantService(IConfiguration config)
    {
        var host = config["Qdrant:Host"] ?? throw new InvalidOperationException("Qdrant:Host is not configured.");
        var port = int.Parse(config["Qdrant:Port"] ?? throw new InvalidOperationException("Qdrant:Port is not configured."));
        _collection = config["Qdrant:CollectionName"] ?? throw new InvalidOperationException("Qdrant:CollectionName is not configured.");
        _client = new QdrantClient(host, port);
    }

    public async Task EnsureCollectionAsync()
    {
        var collections = await _client.ListCollectionsAsync();
        if (!collections.Any(c => c == _collection))
        {
            await _client.CreateCollectionAsync(_collection, new VectorParams
            {
                Size = VectorSize,
                Distance = Distance.Cosine
            });
        }
    }

    public async Task UpsertAsync(ulong id, float[] vector, string text, string source)
    {
        var point = new PointStruct
        {
            Id = new PointId { Num = id },
            Vectors = vector,
            Payload =
            {
                ["text"] = new Value { StringValue = text },
                ["source"] = new Value { StringValue = source }
            }
        };

        await _client.UpsertAsync(_collection, new[] { point });
    }

    public async Task<List<(string Text, string Source)>> SearchAsync(float[] queryVector, int topK = 3)
    {
        var results = await _client.SearchAsync(_collection, queryVector, limit: (ulong)topK);

        return results.Select(r => (
            Text: r.Payload["text"].StringValue,
            Source: r.Payload["source"].StringValue
        )).ToList();
    }
}
