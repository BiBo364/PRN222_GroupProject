using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Helpers;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;

namespace Assignment1_Service.Services;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;

    public SubjectService(ISubjectRepository subjectRepository)
    {
        _subjectRepository = subjectRepository;
    }

    public async Task<List<SubjectListItemDto>> GetSubjectsAsync()
    {
        var subjects = await _subjectRepository.GetSubjectsWithDetailsAsync();

        return subjects.Select(subject => new SubjectListItemDto
        {
            Id = subject.Id,
            Code = subject.Code,
            Name = subject.Name,
            Description = subject.Description,
            ChapterCount = subject.Chapters.Count,
            DocumentCount = subject.Documents.Count,
            IndexedDocumentCount = subject.Documents.Count(document => document.Status == "indexed")
        }).ToList();
    }

    public async Task<SubjectDetailDto?> GetSubjectAsync(int id)
    {
        var subject = await _subjectRepository.GetByIdWithDetailsAsync(id);
        if (subject is null)
            return null;

        return new SubjectDetailDto
        {
            Subject = DtoMapper.ToDto(subject),
            Documents = subject.Documents
                .OrderByDescending(document => document.CreatedAt)
                .Select(DtoMapper.ToListItemDto)
                .ToList()
        };
    }

    public async Task<SubjectDetailDto> CreateSubjectAsync(string code, string name, string? description = null)
    {
        code = code.Trim().ToUpperInvariant();
        name = name.Trim();
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã môn học là bắt buộc.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên môn học là bắt buộc.", nameof(name));

        var existingSubject = await _subjectRepository.GetByCodeAsync(code);
        if (existingSubject is not null)
        {
            if (existingSubject.IsDeleted == true)
                throw new InvalidOperationException("Mã môn học này đã tồn tại trong Thùng rác. Vui lòng khôi phục thay vì tạo mới.");
            else
                throw new InvalidOperationException("Mã môn học đã tồn tại trong hệ thống.");
        }

        var subject = new Subject
        {
            Code = code,
            Name = name,
            Description = description,
            CreatedAt = DateTime.Now
        };

        subject = await _subjectRepository.AddAsync(subject);

        return new SubjectDetailDto
        {
            Subject = new SubjectDto
            {
                Id = subject.Id,
                Code = subject.Code,
                Name = subject.Name,
                Description = subject.Description,
                Chapters = []
            },
            Documents = []
        };
    }

    public async Task<SubjectDetailDto> UpdateSubjectAsync(int id, string code, string name, string? description = null)
    {
        var subject = await _subjectRepository.GetByIdWithDetailsAsync(id);
        if (subject is null)
            throw new KeyNotFoundException("Không tìm thấy môn học.");

        code = code.Trim().ToUpperInvariant();
        name = name.Trim();
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã môn học là bắt buộc.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên môn học là bắt buộc.", nameof(name));

        var existingWithCode = await _subjectRepository.GetByCodeAsync(code);
        if (existingWithCode is not null && existingWithCode.Id != id)
        {
            if (existingWithCode.IsDeleted == true)
                throw new InvalidOperationException("Mã môn học này đã bị xóa và nằm trong Thùng rác. Vui lòng kiểm tra lại.");
            else
                throw new InvalidOperationException("Mã môn học đã tồn tại trong hệ thống.");
        }

        subject.Code = code;
        subject.Name = name;
        subject.Description = description;

        await _subjectRepository.UpdateAsync(subject);

        return new SubjectDetailDto
        {
            Subject = DtoMapper.ToDto(subject),
            Documents = subject.Documents
                .OrderByDescending(document => document.CreatedAt)
                .Select(DtoMapper.ToListItemDto)
                .ToList()
        };
    }

    public async Task<(bool Success, string? Error)> DeleteSubjectAsync(int id)
    {
        return await DeleteSubjectWithDocumentsAsync(id);
    }

    public async Task<(bool Success, string? Error)> DeleteSubjectWithDocumentsAsync(int id, int? deletedByUserId = null)
    {
        var subject = await _subjectRepository.GetByIdWithDetailsAsync(id);
        if (subject is null)
            return (false, "Không tìm thấy môn học.");

        var deletedAt = DateTime.Now;
        foreach (var doc in subject.Documents)
        {
            doc.IsDeleted = true;
            doc.DeletedAt = deletedAt;
            doc.DeletedBy = deletedByUserId;
        }

        subject.IsDeleted = true;
        subject.DeletedAt = deletedAt;
        subject.DeletedBy = deletedByUserId;
        
        await _subjectRepository.UpdateAsync(subject);
             
        return (true, null);
    }

    public async Task<List<SubjectListItemDto>> GetDeletedSubjectsAsync()
    {
        var subjects = await _subjectRepository.GetDeletedSubjectsAsync();

        return subjects.Select(subject => new SubjectListItemDto
        {
            Id = subject.Id,
            Code = subject.Code,
            Name = subject.Name,
            Description = subject.Description,
            ChapterCount = subject.Chapters.Count,
            DocumentCount = subject.Documents.Count,
            IndexedDocumentCount = subject.Documents.Count(document => document.Status == "indexed"),
            DeletedAt = subject.DeletedAt,
            DeletedByName = subject.DeletedByNavigation?.FullName ?? subject.DeletedByNavigation?.Username
        }).ToList();
    }

    public Task<bool> RestoreSubjectAsync(int id)
    {
        return _subjectRepository.RestoreSubjectAsync(id);
    }
}
