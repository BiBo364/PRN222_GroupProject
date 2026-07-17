namespace RagEdu.Tests.Fakes;

internal sealed class FakeAuditLogRepository : IAuditLogRepository
{
    public List<AuditLog> Logs { get; } = [];
    public int? LastUserId { get; private set; }
    public string? LastCategory { get; private set; }
    public string? LastAction { get; private set; }
    public string? LastSearch { get; private set; }
    public DateTime? LastFromUtc { get; private set; }
    public DateTime? LastToUtc { get; private set; }
    public int LastSkip { get; private set; }
    public int LastTake { get; private set; }
    public int AddCallCount { get; private set; }
    public int CountCallCount { get; private set; }
    public int PageCallCount { get; private set; }
    public int CategoriesCallCount { get; private set; }

    public Task AddAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AddCallCount++;
        if (log.Id == 0)
            log.Id = Logs.Select(item => item.Id).DefaultIfEmpty().Max() + 1;
        Logs.Add(log);
        return Task.CompletedTask;
    }

    public Task<int> CountAsync(
        int? userId,
        string? category,
        string? action,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CountCallCount++;
        Capture(userId, category, action, search, fromUtc, toUtc);
        return Task.FromResult(Filter(userId, category, action, search, fromUtc, toUtc).Count);
    }

    public Task<List<AuditLog>> GetPageAsync(
        int? userId,
        string? category,
        string? action,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PageCallCount++;
        Capture(userId, category, action, search, fromUtc, toUtc);
        LastSkip = skip;
        LastTake = take;
        var page = Filter(userId, category, action, search, fromUtc, toUtc)
            .OrderByDescending(log => log.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList();
        return Task.FromResult(page);
    }

    public Task<List<string>> GetCategoriesAsync(
        int? userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CategoriesCallCount++;
        LastUserId = userId;
        var categories = Logs
            .Where(log => !userId.HasValue || log.UserId == userId)
            .Select(log => log.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category)
            .ToList();
        return Task.FromResult(categories);
    }

    private void Capture(
        int? userId,
        string? category,
        string? action,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        LastUserId = userId;
        LastCategory = category;
        LastAction = action;
        LastSearch = search;
        LastFromUtc = fromUtc;
        LastToUtc = toUtc;
    }

    private List<AuditLog> Filter(
        int? userId,
        string? category,
        string? action,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        IEnumerable<AuditLog> query = Logs;
        if (userId.HasValue)
            query = query.Where(log => log.UserId == userId);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(log => string.Equals(log.Category, category, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(log => string.Equals(log.Action, action, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(log =>
                log.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (log.EntityId?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (log.User?.FullName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (log.User?.Username.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        if (fromUtc.HasValue)
            query = query.Where(log => log.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(log => log.CreatedAt < toUtc.Value);

        return query.ToList();
    }
}
