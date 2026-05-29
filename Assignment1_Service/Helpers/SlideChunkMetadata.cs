using System.Text.Json;
using System.Text.Json.Serialization;

namespace Assignment1_Service.Helpers;

public class SlideChunkMetadata
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "slide";

    [JsonPropertyName("slideNumber")]
    public int SlideNumber { get; set; }

    [JsonPropertyName("imageUrls")]
    public List<string> ImageUrls { get; set; } = new();

    public static string ToJson(SlideChunkMetadata metadata)
    {
        return JsonSerializer.Serialize(metadata);
    }

    public static SlideChunkMetadata? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<SlideChunkMetadata>(json);
        }
        catch
        {
            return null;
        }
    }
}

public class SlideContent
{
    public int SlideNumber { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
}
