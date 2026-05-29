using Assignment1_Repository.Models;
using Assignment1_Service.Models;

namespace Assignment1_Service.Helpers;

public static class ChunkRetriever
{
    private const double MinScore = 0.15;
    private const int DefaultTopK = 5;

    public static List<RetrievedChunk> Retrieve(
        string question,
        IEnumerable<Chunk> chunks,
        int embeddingDimension,
        int topK = DefaultTopK)
    {
        var queryVector = VectorMath.ParseVector(SimpleEmbedder.GenerateVector(question, embeddingDimension));

        var scored = chunks
            .Select(chunk =>
            {
                var embedding = chunk.Embeddings.FirstOrDefault();
                if (embedding is null)
                    return null;

                var chunkVector = VectorMath.ParseVector(embedding.Vector);
                var score = VectorMath.CosineSimilarity(queryVector, chunkVector);
                return new RetrievedChunk
                {
                    Chunk = chunk,
                    Document = chunk.Document,
                    Score = score
                };
            })
            .Where(x => x is not null)
            .Cast<RetrievedChunk>()
            .Where(x => x.Score >= MinScore)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();

        return scored;
    }
}
