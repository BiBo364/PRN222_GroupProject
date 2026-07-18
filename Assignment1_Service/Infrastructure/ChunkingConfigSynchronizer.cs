using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Models;
using Microsoft.Extensions.Logging;

namespace Assignment1_Service.Infrastructure;

/// <summary>
/// Seeds the default chunking configuration from appsettings only when the database has no configuration.
/// </summary>
public static class ChunkingConfigSynchronizer
{
    /// <summary>
    /// Preserves database settings created by an administrator across application restarts.
    /// </summary>
    public static async Task SyncAsync(
        IDocumentRepository documentRepository,
        ChunkingSettings settings,
        ILogger logger)
    {
        var existing = await documentRepository.GetFirstChunkingConfigAsync();
        if (existing is not null)
        {
            logger.LogInformation(
                "Keeping database chunking configuration: Id={Id}, ChunkSize={ChunkSize}, Overlap={Overlap}.",
                existing.Id,
                existing.ChunkSize,
                existing.ChunkOverlap);
            return;
        }

        var config = await documentRepository.UpsertChunkingConfigAsync(
            name: "appsettings-config",
            strategy: "fixed",
            chunkSize: settings.MaxWordsPerChunk,
            chunkOverlap: settings.OverlapWords,
            description: $"Default chunking configuration seeded from appsettings.json: {settings.MaxWordsPerChunk} words/chunk, overlap {settings.OverlapWords} words.");

        logger.LogInformation(
            "Seeded default chunking configuration in database: Id={Id}, ChunkSize={ChunkSize}, Overlap={Overlap}.",
            config.Id,
            config.ChunkSize,
            config.ChunkOverlap);
    }
}
