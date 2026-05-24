using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Services.Interfaces;

namespace Assignment1_Service.Services;

public class UserServices : IUserServices
{
    private readonly IUserReposity _userRepository;
    private readonly RagEduContext _context;

    public UserServices(IUserReposity userRepository, RagEduContext context)
    {
        _userRepository = userRepository;
        _context = context;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user is null || user.IsActive == false)
            return null;

        if (user.Password != password)
            return null;

        user.LastLoginAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return user;
    }
}
