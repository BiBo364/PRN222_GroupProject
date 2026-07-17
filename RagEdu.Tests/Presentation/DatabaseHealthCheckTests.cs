using Assignment1_Repository.Models;
using Assignmet1_Presentation.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RagEdu.Tests.Presentation;

public sealed class DatabaseHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthyForReachableInMemoryDatabase()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var healthCheck = new DatabaseHealthCheck(context);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.Description));
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthyWhenContextWasDisposed()
    {
        var context = CreateContext();
        await context.DisposeAsync();
        var healthCheck = new DatabaseHealthCheck(context);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
        Assert.IsType<ObjectDisposedException>(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_ObservesCancelledTokenWithoutThrowingToCaller()
    {
        await using var context = CreateContext();
        var healthCheck = new DatabaseHealthCheck(context);
        using var source = new CancellationTokenSource();
        source.Cancel();

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(),
            source.Token);

        Assert.Contains(
            result.Status,
            new[] { HealthStatus.Healthy, HealthStatus.Unhealthy });
    }

    private static RagEduContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RagEduContext>()
            .UseInMemoryDatabase($"health-{Guid.NewGuid():N}")
            .Options;
        return new RagEduContext(options);
    }
}
