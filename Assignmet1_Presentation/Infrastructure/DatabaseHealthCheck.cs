using Assignment1_Repository.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Assignmet1_Presentation.Infrastructure;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly RagEduContext _context;

    public DatabaseHealthCheck(RagEduContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Kết nối cơ sở dữ liệu hoạt động bình thường.")
                : HealthCheckResult.Unhealthy("Không thể kết nối cơ sở dữ liệu.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Kiểm tra kết nối cơ sở dữ liệu thất bại.",
                exception);
        }
    }
}
