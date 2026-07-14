using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface ISubjectService
{
    Task<List<SubjectListItemDto>> GetSubjectsAsync();
    Task<SubjectDetailDto?> GetSubjectAsync(int id);
    Task<SubjectDetailDto> CreateSubjectAsync(string code, string name, string? description = null);
    Task<SubjectDetailDto> UpdateSubjectAsync(int id, string code, string name, string? description = null);
    Task<(bool Success, string? Error)> DeleteSubjectAsync(int id);
    Task<(bool Success, string? Error)> DeleteSubjectWithDocumentsAsync(int id, int? deletedByUserId = null);
    Task<List<SubjectListItemDto>> GetDeletedSubjectsAsync();
    Task<bool> RestoreSubjectAsync(int id);
}