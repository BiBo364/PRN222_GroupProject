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
        return _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);
    }

    public Task<User?> GetByIdAsync(int id)
    {
        return _context.Users
            .Include(u => u.Role)
            .Include(u => u.Subject)
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
            .OrderBy(u => u.RoleId)
            .ThenBy(u => u.FullName)
            .ToListAsync();
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

