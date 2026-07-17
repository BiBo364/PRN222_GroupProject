namespace RagEdu.Tests.Fakes;

internal sealed class FakeUserRepository : IUserReposity
{
    public List<User> Users { get; } = [];

    public Task<User?> GetByUsernameAsync(string username)
    {
        return Task.FromResult(Users.FirstOrDefault(
            user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<User?> GetByIdAsync(int id)
    {
        return Task.FromResult(Users.FirstOrDefault(user => user.Id == id));
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return Task.FromResult(Users.FirstOrDefault(
            user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<List<User>> GetAllAsync()
    {
        return Task.FromResult(Users.ToList());
    }

    public Task<User?> GetTeacherAssignedToSubjectAsync(int subjectId, int? excludeUserId = null)
    {
        return Task.FromResult(Users.FirstOrDefault(user =>
            user.RoleId == 2
            && user.SubjectId == subjectId
            && user.Id != excludeUserId));
    }

    public Task<User> AddAsync(User user)
    {
        if (user.Id == 0)
            user.Id = Users.Select(item => item.Id).DefaultIfEmpty().Max() + 1;

        Users.Add(user);
        return Task.FromResult(user);
    }

    public Task UpdateAsync(User user)
    {
        var index = Users.FindIndex(item => item.Id == user.Id);
        if (index >= 0)
            Users[index] = user;
        else
            Users.Add(user);

        return Task.CompletedTask;
    }
}
