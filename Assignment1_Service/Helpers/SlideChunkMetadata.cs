using System.Text.Json;
using System.Text.Json.Serialization;

namespace Assignment1_Service.Helpers;

public class SlideChunkMetadata
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("slideNumber")]
    public int? SlideNumber { get; set; }

    [JsonPropertyName("pageNumber")]
    public int? PageNumber { get; set; }

    [JsonPropertyName("imageUrls")]
    public List<string> ImageUrls { get; set; } = new();

    [JsonIgnore]
    public bool IsSlide => string.Equals(Type, "slide", StringComparison.OrdinalIgnoreCase)
                           || NormalizeNumber(SlideNumber).HasValue
                           || ImageUrls.Count > 0;

    [JsonIgnore]
    public int? EffectiveSlideNumber
    {
        get
        {
            var slideNumber = NormalizeNumber(SlideNumber);
            if (slideNumber.HasValue)
                return slideNumber;

            return IsSlide ? NormalizeNumber(PageNumber) : null;
        }
    }

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
            return JsonSerializer.Deserialize<SlideChunkMetadata>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static int? NormalizeNumber(int? value)
    {
        return value.GetValueOrDefault() > 0 ? value : null;
    }
}

public class SlideContent
{
    public int SlideNumber { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
}
