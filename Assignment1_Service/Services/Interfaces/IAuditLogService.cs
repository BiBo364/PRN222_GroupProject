using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IAuditLogService
{
    Task RecordAsync(
        RecordAuditLogRequest request,
        CancellationToken cancellationToken = default);
    Task<AuditLogPageDto> GetPageAsync(
        int requesterUserId,
        int requesterRoleId,
        AuditLogQuery query,
        CancellationToken cancellationToken = default);
}
