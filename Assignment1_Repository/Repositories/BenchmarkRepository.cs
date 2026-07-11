using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Assignment1_Repository.Repositories;

public class BenchmarkRepository : IBenchmarkRepository
{
    private readonly RagEduContext _context;

    public BenchmarkRepository(RagEduContext context)
    {
        _context = context;
    }

    public Task<List<TestQuestion>> GetTestQuestionsAsync(int? subjectId = null)
    {
        var query = _context.TestQuestions
            .AsNoTracking()
            .Include(q => q.Subject)
            .Include(q => q.Chapter)
            .AsQueryable();

        if (subjectId.HasValue)
            query = query.Where(q => q.SubjectId == subjectId.Value);

        return query
            .OrderByDescending(q => q.CreatedAt)
            .ThenByDescending(q => q.Id)
            .ToListAsync();
    }

    public Task<TestQuestion?> GetTestQuestionByIdAsync(int id)
    {
        return _context.TestQuestions
            .Include(q => q.Subject)
            .Include(q => q.Chapter)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<TestQuestion> AddTestQuestionAsync(TestQuestion question)
    {
        _context.TestQuestions.Add(question);
        await _context.SaveChangesAsync();
        return question;
    }

    public async Task UpdateTestQuestionAsync(TestQuestion question)
    {
        var existing = await _context.TestQuestions.FindAsync(question.Id)
            ?? throw new InvalidOperationException("Test question not found.");

        existing.SubjectId = question.SubjectId;
        existing.ChapterId = question.ChapterId;
        existing.Question = question.Question;
        existing.GroundTruth = question.GroundTruth;
        existing.GroundTruthChunks = question.GroundTruthChunks;
        existing.Difficulty = question.Difficulty;
        existing.Category = question.Category;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteTestQuestionAsync(int id)
    {
        var existing = await _context.TestQuestions.FindAsync(id);
        if (existing is null)
            return;

        _context.TestQuestions.Remove(existing);
        await _context.SaveChangesAsync();
    }

    public Task<List<BenchmarkRun>> GetBenchmarkRunsAsync(int take = 50)
    {
        return _context.BenchmarkRuns
            .AsNoTracking()
            .Include(r => r.BenchmarkSummary)
            .Include(r => r.EmbeddingModel)
            .Include(r => r.ChunkingConfig)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Take(take)
            .ToListAsync();
    }

    public Task<BenchmarkRun?> GetBenchmarkRunDetailAsync(int runId)
    {
        return _context.BenchmarkRuns
            .AsNoTracking()
            .Include(r => r.BenchmarkSummary)
            .Include(r => r.EmbeddingModel)
            .Include(r => r.ChunkingConfig)
            .Include(r => r.BenchmarkResults)
                .ThenInclude(result => result.Question)
                    .ThenInclude(question => question.Subject)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == runId);
    }

    public async Task<BenchmarkRun> CreateBenchmarkRunAsync(BenchmarkRun run)
    {
        _context.BenchmarkRuns.Add(run);
        await _context.SaveChangesAsync();
        return run;
    }

    public async Task UpdateBenchmarkRunAsync(BenchmarkRun run)
    {
        var existing = await _context.BenchmarkRuns.FindAsync(run.Id)
            ?? throw new InvalidOperationException("Benchmark run not found.");

        existing.Status = run.Status;
        existing.StartedAt = run.StartedAt;
        existing.FinishedAt = run.FinishedAt;

        await _context.SaveChangesAsync();
    }

    public async Task AddBenchmarkResultAsync(BenchmarkResult result)
    {
        _context.BenchmarkResults.Add(result);
        await _context.SaveChangesAsync();
    }

    public async Task SaveBenchmarkSummaryAsync(BenchmarkSummary summary)
    {
        _context.BenchmarkSummaries.Add(summary);
        await _context.SaveChangesAsync();
    }

    public Task<List<ChunkingConfig>> GetChunkingConfigsAsync()
    {
        return _context.ChunkingConfigs
            .AsNoTracking()
            .OrderBy(config => config.Name)
            .ToListAsync();
    }

    public Task<List<EmbeddingModel>> GetEmbeddingModelsAsync()
    {
        return _context.EmbeddingModels
            .AsNoTracking()
            .OrderBy(model => model.Id)
            .ToListAsync();
    }

    public Task<List<Subject>> GetSubjectsWithTestQuestionsAsync()
    {
        return _context.Subjects
            .AsNoTracking()
            .Where(subject => subject.TestQuestions.Any())
            .OrderBy(subject => subject.Code)
            .ToListAsync();
    }
}
