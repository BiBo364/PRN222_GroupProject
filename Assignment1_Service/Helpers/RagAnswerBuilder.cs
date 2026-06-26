using Assignment1_Service.Models;

namespace Assignment1_Service.Helpers;

public static class RagAnswerBuilder
{
    public static ChatReplyDto Build(string question, List<RetrievedChunk> chunks)
    {
        if (chunks.Count == 0)
        {
            return new ChatReplyDto
            {
                FoundInDocuments = false,
                Answer = "I could not find relevant information in the uploaded course materials for this question. Please ask about content from indexed documents only."
            };
        }

        var citations = chunks.Select((c, i) =>
        {
            var slideMeta = SlideChunkMetadata.FromJson(c.Chunk.Metadata);
            var pageNumber = c.Chunk.PageNumber.GetValueOrDefault() > 0
                ? c.Chunk.PageNumber
                : null;

            return new ChatCitationDto
            {
                ChunkId = c.Chunk.Id,
                DocumentName = c.Document.OriginalName,
                SlideNumber = slideMeta?.EffectiveSlideNumber ?? pageNumber,
                Excerpt = Truncate(c.Chunk.Content, 200),
                Score = Math.Round(c.Score, 3)
            };
        }).ToList();

        var context = string.Join("\n\n", chunks.Select((c, i) =>
        {
            var label = FormatSource(c, i + 1);
            return $"[{label}]\n{c.Chunk.Content}";
        }));

        var answer =
            $"Based on the course materials:\n\n{SummarizeFromContext(question, context)}\n\n" +
            "Sources: " + string.Join("; ", citations.Select(c =>
                c.SlideNumber.HasValue
                    ? $"{c.DocumentName} (slide {c.SlideNumber})"
                    : c.DocumentName));

        return new ChatReplyDto
        {
            FoundInDocuments = true,
            Answer = answer,
            Citations = citations
        };
    }

    private static string SummarizeFromContext(string question, string context)
    {
        var sentences = context
            .Split(new[] { '\n', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 20)
            .Take(6)
            .ToList();

        if (sentences.Count == 0)
            return context.Length > 600 ? context[..600] + "..." : context;

        return string.Join(" ", sentences);
    }

    private static string FormatSource(RetrievedChunk chunk, int index)
    {
        var slideMeta = SlideChunkMetadata.FromJson(chunk.Chunk.Metadata);
        if (slideMeta?.EffectiveSlideNumber is int slideNum)
            return $"Source {index}: {chunk.Document.OriginalName}, slide {slideNum}";

        return $"Source {index}: {chunk.Document.OriginalName}";
    }

    private static string Truncate(string text, int max)
    {
        if (text.Length <= max)
            return text;
        return text[..max] + "...";
    }
}
