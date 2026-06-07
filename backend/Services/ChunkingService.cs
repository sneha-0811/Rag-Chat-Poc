namespace backend.Services;

public class ChunkingService
{
    private const int ChunkSize = 500;
    private const int Overlap = 50;

    public List<string> Chunk(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();

        int i = 0;
        while (i < words.Length)
        {
            var chunk = words.Skip(i).Take(ChunkSize);
            chunks.Add(string.Join(' ', chunk));
            i += ChunkSize - Overlap;
        }

        return chunks;
    }
}
