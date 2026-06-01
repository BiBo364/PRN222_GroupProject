using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface ISubjectService
{
    Task<List<SubjectListItemDto>> GetSubjectsAsync();
    Task<SubjectDetailDto?> GetSubjectAsync(int id);
    Task<SubjectDetailDto> CreateSubjectAsync(string code, string name, string? description = null);
}