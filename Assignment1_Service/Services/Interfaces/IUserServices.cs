using Assignment1_Repository.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IUserServices
{
    Task<User?> LoginAsync(string username, string password);
}
