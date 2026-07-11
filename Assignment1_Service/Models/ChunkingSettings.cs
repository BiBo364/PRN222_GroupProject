namespace Assignment1_Service.Models;

/// <summary>
/// Cấu hình tham số chunking tài liệu.
/// Để điều chỉnh, hãy sửa section "Chunking" trong appsettings.json.
/// Không cần đụng vào database hay code.
/// </summary>
public class ChunkingSettings
{
    public const string SectionName = "Chunking";

    // ── Cấu hình kích thước chunk (tính theo số TỪ) ─────────────────
    /// <summary>
    /// Số từ tối đa trong mỗi chunk.
    /// Giá trị khuyến nghị: 150–400 từ.
    /// Tăng lên để mỗi chunk chứa nhiều ngữ cảnh hơn (tốt cho câu hỏi tổng quát).
    /// Giảm xuống để chunk nhỏ hơn, câu trả lời chính xác hơn (tốt cho câu hỏi chi tiết).
    /// </summary>
    public int MaxWordsPerChunk { get; set; } = 250;

    /// <summary>
    /// Số từ được lặp lại giữa hai chunk liền kề (overlap).
    /// Giúp tránh mất ngữ cảnh ở ranh giới giữa hai chunk.
    /// Thường bằng 10–20% của MaxWordsPerChunk.
    /// </summary>
    public int OverlapWords { get; set; } = 40;

    // ── Cấu hình retrieval ───────────────────────────────────────────
    /// <summary>
    /// Số chunk tối đa được truy xuất cho mỗi câu hỏi (Top-K).
    /// Tăng lên nếu câu trả lời thường thiếu thông tin.
    /// Giảm xuống nếu cần tiết kiệm token Gemini.
    /// </summary>
    public int TopK { get; set; } = 4;

    /// <summary>
    /// Bật/tắt hybrid re-ranking (kết hợp vector similarity + keyword matching).
    /// true  → chính xác hơn nhưng tốn thêm một chút CPU.
    /// false → chỉ dùng vector similarity (mặc định).
    /// </summary>
    public bool UseHybridRerank { get; set; } = false;
}
