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
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (await _subjectRepository.GetByCodeAsync(code) is not null)
            throw new InvalidOperationException("Subject code already exists.");

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
}