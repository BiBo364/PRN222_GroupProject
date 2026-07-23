using System.Text;
using Assignment1_Service.Helpers;

namespace Assignmet1_Presentation.Models;

public class ChunkDisplayItem
{
    public ChunkViewModel Chunk { get; set; } = null!;
    public IReadOnlyList<ChunkViewModel> Chunks { get; set; } = [];
    public bool IsSlide { get; set; }
    public int DisplayIndex { get; set; }
    public int? SlideNumber { get; set; }
    public int? PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public List<string> ImageUrls { get; set; } = [];
    public int ChunkCount => Chunks.Count;

    public static List<ChunkDisplayItem> Build(IEnumerable<ChunkViewModel> chunks, bool isSlideDeck)
    {
        var orderedChunks = chunks
            .OrderBy(chunk => chunk.ChunkIndex)
            .ToList();

        if (orderedChunks.Count == 0)
            return [];

        return isSlideDeck
            ? BuildSlideItems(orderedChunks)
            : BuildPageItems(orderedChunks);
    }

    private static List<ChunkDisplayItem> BuildSlideItems(IReadOnlyList<ChunkViewModel> chunks)
    {
        var resolvedSlideNumbers = chunks
            .Select(ResolveSlideNumber)
            .Where(number => number.HasValue)
            .Select(number => number!.Value)
            .Distinct()
            .Count();

        if (chunks.Count > 1 && resolvedSlideNumbers <= 1)
        {
            return chunks
                .OrderBy(chunk => chunk.ChunkIndex)
                .Select((chunk, index) => BuildSlideItem([chunk], index, index + 1))
                .ToList();
        }

        return chunks
            .GroupBy(chunk => ResolveSlideNumber(chunk) ?? chunk.ChunkIndex + 1)
            .OrderBy(group => group.Key)
            .Select((group, index) =>
            {
                var groupChunks = group
                    .OrderBy(chunk => chunk.ChunkIndex)
                    .ToList();

                var slideNumber = NormalizeNumber(group.Key) ?? index + 1;
                return BuildSlideItem(groupChunks, index, slideNumber);
            })
            .ToList();
    }

    private static ChunkDisplayItem BuildSlideItem(IReadOnlyList<ChunkViewModel> groupChunks, int index, int slideNumber)
    {
        var imageUrls = groupChunks
            .Select(chunk => SlideChunkMetadata.FromJson(chunk.Metadata))
            .Where(metadata => metadata is not null)
            .SelectMany(metadata => metadata!.ImageUrls)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var content = MergeChunkContent(groupChunks);

        return new ChunkDisplayItem
        {
            Chunk = groupChunks[0],
            Chunks = groupChunks,
            IsSlide = true,
            DisplayIndex = index + 1,
            SlideNumber = slideNumber,
            PageNumber = slideNumber,
            Content = content,
            TokenCount = CountTokens(content),
            ImageUrls = imageUrls
        };
    }

    private static List<ChunkDisplayItem> BuildPageItems(IReadOnlyList<ChunkViewModel> chunks)
    {
        return chunks
            .OrderBy(chunk => chunk.ChunkIndex)
            .Select((chunk, index) =>
            {
                return new ChunkDisplayItem
                {
                    Chunk = chunk,
                    Chunks = [chunk],
                    IsSlide = false,
                    DisplayIndex = index + 1,
                    PageNumber = NormalizeNumber(chunk.PageNumber),
                    Content = NormalizeDisplayText(chunk.Content),
                    TokenCount = chunk.TokenCount ?? CountTokens(chunk.Content),
                    ImageUrls = []
                };
            })
            .ToList();
    }

    private static int? ResolveSlideNumber(ChunkViewModel chunk)
    {
        var metadata = SlideChunkMetadata.FromJson(chunk.Metadata);
        return metadata?.EffectiveSlideNumber ?? NormalizeNumber(chunk.PageNumber);
    }

    private static int CountTokens(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static string MergeChunkContent(IReadOnlyList<ChunkViewModel> chunks)
    {
        if (chunks.Count == 0)
            return string.Empty;

        var orderedChunks = chunks
            .OrderBy(chunk => chunk.CharStart ?? int.MaxValue)
            .ThenBy(chunk => chunk.ChunkIndex)
            .ToList();

        if (orderedChunks.Count == 1)
            return NormalizeDisplayText(orderedChunks[0].Content);

        if (!orderedChunks.All(chunk => chunk.CharStart.HasValue && chunk.CharEnd.HasValue))
        {
            return string.Join(
                "\n\n",
                orderedChunks
                    .Select(chunk => NormalizeDisplayText(chunk.Content))
                    .Where(content => !string.IsNullOrWhiteSpace(content)));
        }

        var builder = new StringBuilder();
        var lastEnd = orderedChunks[0].CharStart!.Value;

        foreach (var chunk in orderedChunks)
        {
            var content = chunk.Content ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content))
                continue;

            var charStart = chunk.CharStart!.Value;
            var charEnd = chunk.CharEnd!.Value;
            var textToAppend = content;
            var separateBlock = false;

            if (builder.Length > 0)
            {
                if (charStart < lastEnd)
                {
                    var overlap = lastEnd - charStart;
                    if (overlap >= textToAppend.Length)
                    {
                        lastEnd = Math.Max(lastEnd, charEnd);
                        continue;
                    }

                    textToAppend = textToAppend[overlap..];
                }
                else if (charStart > lastEnd)
                {
                    separateBlock = true;
                }
            }

            AppendReadableText(builder, textToAppend, separateBlock);
            lastEnd = Math.Max(lastEnd, charEnd);
        }

        return NormalizeDisplayText(builder.ToString());
    }

    private static void AppendReadableText(StringBuilder builder, string text, bool separateBlock)
    {
        var normalized = NormalizeDisplayText(text);
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        if (builder.Length > 0)
        {
            if (separateBlock)
            {
                builder.AppendLine();
                builder.AppendLine();
            }
            else if (NeedsSpace(builder[builder.Length - 1], normalized[0]))
            {
                builder.Append(' ');
            }
        }

        builder.Append(normalized);
    }

    private static bool NeedsSpace(char previous, char next)
    {
        return !char.IsWhiteSpace(previous)
               && !char.IsWhiteSpace(next)
               && !char.IsPunctuation(next)
               && next != ')'
               && next != ']';
    }

    private static string NormalizeDisplayText(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Replace("\r\n", "\n").Trim();
    }

    private static int? NormalizeNumber(int? value)
    {
        return value.GetValueOrDefault() > 0 ? value : null;
    }
}
