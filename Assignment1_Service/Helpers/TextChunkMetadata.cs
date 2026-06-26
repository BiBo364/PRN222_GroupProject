using System.Text.Json;
using System.Text.Json.Serialization;

namespace Assignment1_Service.Helpers;

public sealed class TextChunkMetadata
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    public static string ToJson(TextChunkMetadata metadata)
    {
        return JsonSerializer.Serialize(metadata);
    }
}
