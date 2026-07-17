using Assignment1_Repository.Models;

namespace Assignment1_Repository.Repositories.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<int> CountAsync(
        int? userId,
        string? category,
        string? action,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);
    Task<List<AuditLog>> GetPageAsync(
        int? userId,
        string? category,
        string? action,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task<List<string>> GetCategoriesAsync(
        int? userId,
        CancellationToken cancellationToken = default);
}
