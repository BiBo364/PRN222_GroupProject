using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Assignment1_Repository.Repositories;

public class SubjectRepository : ISubjectRepository
{
    private readonly RagEduContext _context;

    public SubjectRepository(RagEduContext context)
    {
        _context = context;
    }

    public Task<List<Subject>> GetSubjectsWithDetailsAsync()
    {
        return _context.Subjects
            .Include(subject => subject.Chapters)
            .Include(subject => subject.Documents)
            .OrderBy(subject => subject.Code)
            .ToListAsync();
    }

    public Task<Subject?> GetByIdWithDetailsAsync(int id)
    {
        return _context.Subjects
            .Include(subject => subject.Chapters.OrderBy(chapter => chapter.Number))
            .Include(subject => subject.Documents)
                .ThenInclude(document => document.Chapter)
            .Include(subject => subject.Documents)
                .ThenInclude(document => document.Chunks)
            .FirstOrDefaultAsync(subject => subject.Id == id);
    }

    public Task<Subject?> GetByCodeAsync(string code)
    {
        code = code.Trim().ToUpperInvariant();
        return _context.Subjects.FirstOrDefaultAsync(subject => subject.Code == code);
    }

    public async Task<Subject> AddAsync(Subject subject)
    {
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();
        return subject;
    }

    public async Task UpdateAsync(Subject subject)
    {
        _context.Subjects.Update(subject);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject is null) return false;

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();
        return true;
    }
}