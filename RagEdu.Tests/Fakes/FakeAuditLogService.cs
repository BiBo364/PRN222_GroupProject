namespace RagEdu.Tests.Fakes;

internal sealed class FakeAuditLogService : IAuditLogService
{
    public List<RecordAuditLogRequest> RecordedRequests { get; } = [];
    public AuditLogPageDto PageResult { get; set; } = new();
    public Exception? RecordException { get; set; }
    public Exception? PageException { get; set; }

    public Task RecordAsync(
        RecordAuditLogRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (RecordException is not null)
            throw RecordException;
        RecordedRequests.Add(request);
        return Task.CompletedTask;
    }

    public Task<AuditLogPageDto> GetPageAsync(
        int requesterUserId,
        int requesterRoleId,
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (PageException is not null)
            throw PageException;
        return Task.FromResult(PageResult);
    }
}
