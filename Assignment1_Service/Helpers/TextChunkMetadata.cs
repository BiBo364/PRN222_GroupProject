using System.Text.Json;

namespace Assignment1_Service.Helpers;

public sealed class TextChunkMetadata
{
    public string Type { get; set; } = "text";
    public int PageNumber { get; set; }

    public static string ToJson(TextChunkMetadata metadata)
    {
        return JsonSerializer.Serialize(metadata);
    }
}