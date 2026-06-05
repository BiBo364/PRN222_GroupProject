using Assignment1_Repository.Models;

namespace Assignment1_Repository.Repositories.Interfaces;

public interface ISubjectRepository
{
    Task<List<Subject>> GetSubjectsWithDetailsAsync();
    Task<Subject?> GetByIdWithDetailsAsync(int id);
    Task<Subject?> GetByCodeAsync(string code);
    Task<Subject> AddAsync(Subject subject);
    Task UpdateAsync(Subject subject);
    Task<bool> DeleteAsync(int id);
}