using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IUserServices
{
    Task<LoginUserDto?> LoginAsync(string username, string password);
}
