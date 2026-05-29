using Assignment1_Repository.Models;

namespace Assignment1_Service.Models;

public class RetrievedChunk
{
    public Chunk Chunk { get; set; } = null!;
    public Document Document { get; set; } = null!;
    public double Score { get; set; }
}
