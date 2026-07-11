using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Assignment1_Service.Infrastructure;

/// <summary>
/// Đồng bộ cấu hình chunking từ appsettings.json vào database khi khởi động.
/// Đảm bảo mọi thay đổi trong appsettings.json đều được phản ánh vào bảng chunking_configs.
/// </summary>
public static class ChunkingConfigSynchronizer
{
    /// <summary>
    /// Gọi khi khởi động app. Đọc ChunkingSettings từ appsettings.json và lưu/cập nhật vào DB.
    /// </summary>
    public static async Task SyncAsync(
        IDocumentRepository documentRepository,
        ChunkingSettings settings,
        ILogger logger)
    {
        var description =
            $"Cấu hình chunk từ appsettings.json: " +
            $"{settings.MaxWordsPerChunk} từ/chunk, " +
            $"overlap {settings.OverlapWords} từ, " +
            $"TopK={settings.TopK}, " +
            $"HybridRerank={settings.UseHybridRerank}";

        var config = await documentRepository.UpsertChunkingConfigAsync(
            name: "appsettings-config",
            strategy: "fixed",
            chunkSize: settings.MaxWordsPerChunk,
            chunkOverlap: settings.OverlapWords,
            description: description);

        logger.LogInformation(
            "Chunking config đã được đồng bộ vào DB: Id={Id}, ChunkSize={ChunkSize} từ, Overlap={Overlap} từ.",
            config.Id,
            config.ChunkSize,
            config.ChunkOverlap);
    }
}
