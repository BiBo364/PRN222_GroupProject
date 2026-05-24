using Assignment1_Repository.Models;

namespace Assignment1_Repository.Repositories.Interfaces;

public interface IUserReposity
{
    Task<User?> GetByUsernameAsync(string username);
}
