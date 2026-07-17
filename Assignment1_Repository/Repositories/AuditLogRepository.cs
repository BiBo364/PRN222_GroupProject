using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Assignment1_Repository.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly RagEduContext _context;

    public AuditLogRepository(RagEduContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        await _context.AuditLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
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
        return BuildQuery(userId, category, action, search, fromUtc, toUtc)
            .CountAsync(cancellationToken);
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
        return BuildQuery(userId, category, action, search, fromUtc, toUtc)
            .Include(log => log.User)
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public Task<List<string>> GetCategoriesAsync(
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking();
        if (userId.HasValue)
            query = query.Where(log => log.UserId == userId.Value);

        return query
            .Select(log => log.Category)
            .Distinct()
            .OrderBy(category => category)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<AuditLog> BuildQuery(
        int? userId,
        string? category,
        string? action,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (userId.HasValue)
            query = query.Where(log => log.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(log => log.Category == category);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(log => log.Action == action);
        if (fromUtc.HasValue)
            query = query.Where(log => log.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(log => log.CreatedAt < toUtc.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(log =>
                log.Description.Contains(keyword)
                || (log.EntityId != null && log.EntityId.Contains(keyword))
                || (log.User != null
                    && ((log.User.FullName != null && log.User.FullName.Contains(keyword))
                        || log.User.Username.Contains(keyword))));
        }

        return query;
    }
}
