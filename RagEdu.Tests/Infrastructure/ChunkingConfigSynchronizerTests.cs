using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories;
using Assignment1_Service.Infrastructure;
using Assignment1_Service.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace RagEdu.Tests.Infrastructure;

public class ChunkingConfigSynchronizerTests
{
    [Fact]
    public async Task SyncAsync_WhenAdminConfigurationExists_DoesNotOverwriteDatabaseValues()
    {
        var options = new DbContextOptionsBuilder<RagEduContext>()
            .UseInMemoryDatabase($"chunking-config-{Guid.NewGuid():N}")
            .Options;

        await using var context = new RagEduContext(options);
        context.ChunkingConfigs.Add(new ChunkingConfig
        {
            Name = "admin-config",
            Strategy = "fixed",
            ChunkSize = 320,
            ChunkOverlap = 40,
            Description = "Configured by an administrator"
        });
        await context.SaveChangesAsync();

        var repository = new DocumentRepository(context);
        var appSettings = new ChunkingSettings { MaxWordsPerChunk = 250, OverlapWords = 40 };

        await ChunkingConfigSynchronizer.SyncAsync(repository, appSettings, NullLogger.Instance);

        var savedConfig = await repository.GetFirstChunkingConfigAsync();

        Assert.NotNull(savedConfig);
        Assert.Equal(320, savedConfig.ChunkSize);
        Assert.Equal("admin-config", savedConfig.Name);
    }
}
