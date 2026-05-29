using Assignment1_Repository.Models;
using Assignment1_Service.Helpers;

namespace Assignmet1_Presentation.Models;

public class ChunkDisplayItem
{
    public Chunk Chunk { get; set; } = null!;
    public bool IsSlide { get; set; }
    public int? SlideNumber { get; set; }
    public List<string> ImageUrls { get; set; } = new();

    public static ChunkDisplayItem FromChunk(Chunk chunk)
    {
        var meta = SlideChunkMetadata.FromJson(chunk.Metadata);

        return new ChunkDisplayItem
        {
            Chunk = chunk,
            IsSlide = meta?.Type == "slide",
            SlideNumber = meta?.SlideNumber ?? chunk.PageNumber,
            ImageUrls = meta?.ImageUrls ?? new List<string>()
        };
    }
}
