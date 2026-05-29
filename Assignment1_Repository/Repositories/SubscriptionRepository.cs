using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Assignment1_Repository.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly RagEduContext _context;

    public SubscriptionRepository(RagEduContext context)
    {
        _context = context;
    }

    public Task<List<SubscriptionPlan>> GetActivePlansAsync()
    {
        return _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync();
    }

    public Task<SubscriptionPlan?> GetPlanByIdAsync(int planId)
    {
        return _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
    }

    public Task<UserSubscription?> GetActiveSubscriptionAsync(int userId)
    {
        var now = DateTime.UtcNow;
        return _context.UserSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.IsActive && s.EndAt > now)
            .OrderByDescending(s => s.EndAt)
            .FirstOrDefaultAsync();
    }

    public Task<List<PaymentTicket>> GetTicketsByUserAsync(int userId)
    {
        return _context.PaymentTickets
            .Include(t => t.Plan)
            .Include(t => t.ReviewedByNavigation)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public Task<List<PaymentTicket>> GetAllTicketsAsync(string? status = null)
    {
        var query = _context.PaymentTickets
            .Include(t => t.User)
            .Include(t => t.Plan)
            .Include(t => t.ReviewedByNavigation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        return query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public Task<PaymentTicket?> GetTicketByIdAsync(int ticketId)
    {
        return _context.PaymentTickets
            .Include(t => t.User)
            .Include(t => t.Plan)
            .FirstOrDefaultAsync(t => t.Id == ticketId);
    }

    public Task<bool> HasPendingTicketAsync(int userId, int planId)
    {
        return _context.PaymentTickets.AnyAsync(t =>
            t.UserId == userId &&
            t.PlanId == planId &&
            t.Status == PaymentTicketStatus.Pending);
    }

    public async Task<PaymentTicket> AddTicketAsync(PaymentTicket ticket)
    {
        ticket.CreatedAt = DateTime.UtcNow;
        _context.PaymentTickets.Add(ticket);
        await _context.SaveChangesAsync();
        return ticket;
    }

    public async Task UpdateTicketAsync(PaymentTicket ticket)
    {
        _context.PaymentTickets.Update(ticket);
        await _context.SaveChangesAsync();
    }

    public async Task<UserSubscription> AddSubscriptionAsync(UserSubscription subscription)
    {
        subscription.CreatedAt = DateTime.UtcNow;
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task UpdateSubscriptionAsync(UserSubscription subscription)
    {
        _context.UserSubscriptions.Update(subscription);
        await _context.SaveChangesAsync();
    }

    public async Task DeactivateExpiredSubscriptionsAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var expired = await _context.UserSubscriptions
            .Where(s => s.UserId == userId && s.IsActive && s.EndAt <= now)
            .ToListAsync();

        foreach (var sub in expired)
            sub.IsActive = false;

        if (expired.Count > 0)
            await _context.SaveChangesAsync();
    }
}
