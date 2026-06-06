using Assignment1_Repository.Models;

namespace Assignment1_Repository.Repositories.Interfaces;

public interface IUserReposity
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<List<User>> GetAllAsync();
    Task<User?> GetTeacherAssignedToSubjectAsync(int subjectId, int? excludeUserId = null);
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
}
