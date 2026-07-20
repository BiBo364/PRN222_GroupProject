using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Assignment1_Repository.Repositories;

public class UserRepository : IUserReposity
{
    private readonly RagEduContext _context;

    public UserRepository(RagEduContext context)
    {
        _context = context;
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        username = username.Trim().ToLowerInvariant();
        return _context.Users
            .FirstOrDefaultAsync(u =>
                u.Username.ToLower() == username
                || u.Email.ToLower() == username);
    }

    public Task<User?> GetByIdAsync(int id)
    {
        return _context.Users
            .Include(u => u.Role)
            .Include(u => u.Subject)
            .Include(u => u.AssignedSubjects)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();
        return _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);
    }

    public Task<List<User>> GetAllAsync()
    {
        return _context.Users
            .Include(u => u.Role)
            .Include(u => u.Subject)
            .Include(u => u.AssignedSubjects.Where(subject => subject.IsDeleted != true))
            .OrderBy(u => u.RoleId == 1 ? 0 : 1)
            .ThenByDescending(u => u.CreatedAt)
            .ThenByDescending(u => u.Id)
            .ToListAsync();
    }

    public Task<User?> GetTeacherAssignedToSubjectAsync(int subjectId, int? excludeUserId = null)
    {
        var query = _context.Users
            .Include(u => u.AssignedSubjects)
            .Where(u => u.RoleId == 2 && u.AssignedSubjects.Any(subject => subject.Id == subjectId));

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return query.FirstOrDefaultAsync();
    }

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.Now;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}

