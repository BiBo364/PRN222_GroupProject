using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories;
using Assignment1_Service.Models;
using Assignment1_Service.Services;
using Assignment1_Service.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RagEdu.Tests.Services;

public class LecturerSubjectAssignmentTests
{
    [Fact]
    public async Task UpdateLecturerSubjectsAsync_AssignsMultipleSubjectsToOneLecturer()
    {
        await using var context = CreateContext();
        var lecturer = AddUser(context, 2, "lecturer-a");
        var subjectOne = AddSubject(context, "PRN222");
        var subjectTwo = AddSubject(context, "SWT301");
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.UpdateLecturerSubjectsAsync(lecturer.Id, [subjectOne.Id, subjectTwo.Id]);

        Assert.True(result.Success);
        Assert.All(
            await context.Subjects.OrderBy(subject => subject.Id).ToListAsync(),
            subject => Assert.Equal(lecturer.Id, subject.LecturerId));
    }

    [Fact]
    public async Task UpdateLecturerSubjectsAsync_RejectsSubjectAlreadyAssignedToAnotherLecturer()
    {
        await using var context = CreateContext();
        var firstLecturer = AddUser(context, 2, "lecturer-a");
        var secondLecturer = AddUser(context, 2, "lecturer-b");
        var subject = AddSubject(context, "PRN222");
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.UpdateLecturerSubjectsAsync(firstLecturer.Id, [subject.Id]);

        var result = await service.UpdateLecturerSubjectsAsync(secondLecturer.Id, [subject.Id]);

        Assert.False(result.Success);
        Assert.Equal(firstLecturer.Id, (await context.Subjects.FindAsync(subject.Id))!.LecturerId);
    }

    private static RagEduContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RagEduContext>()
            .UseInMemoryDatabase($"lecturer-subject-{Guid.NewGuid():N}")
            .Options;
        return new RagEduContext(options);
    }

    private static User AddUser(RagEduContext context, int roleId, string username)
    {
        var user = new User
        {
            Username = username,
            Email = $"{username}@example.test",
            Password = "password",
            RoleId = roleId,
            IsActive = true
        };
        context.Users.Add(user);
        return user;
    }

    private static Subject AddSubject(RagEduContext context, string code)
    {
        var subject = new Subject { Code = code, Name = code, IsDeleted = false };
        context.Subjects.Add(subject);
        return subject;
    }

    private static UserServices CreateService(RagEduContext context)
        => new(
            new UserRepository(context),
            new SubjectRepository(context),
            context,
            new NoOpAccountNotificationService());

    private sealed class NoOpAccountNotificationService : IAccountNotificationService
    {
        public Task<AccountNotificationResult> SendAccountCreatedEmailAsync(string toEmail, string fullName, string username, string temporaryPassword, CancellationToken cancellationToken = default)
            => Task.FromResult(new AccountNotificationResult());

        public Task<AccountNotificationResult> SendDuplicateDocumentNotificationEmailAsync(string toEmail, string fullName, string documentName, string subjectCode, string subjectName, string reason, CancellationToken cancellationToken = default)
            => Task.FromResult(new AccountNotificationResult());
    }
}
