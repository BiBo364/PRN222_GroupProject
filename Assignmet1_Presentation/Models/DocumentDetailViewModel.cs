using Assignment1_Repository.Models;

namespace Assignmet1_Presentation.Models;

public class DocumentDetailViewModel
{
    public Document Document { get; set; } = null!;
    public List<ChunkDisplayItem> ChunkItems { get; set; } = new();
    public bool IsSlideDeck { get; set; }
    public bool CanUpload { get; set; }
    public bool CanDelete { get; set; }
    public bool CanReindex { get; set; }
    public bool NeedsReindex => Document.Status == "indexed" && !Document.Chunks.Any();
}
