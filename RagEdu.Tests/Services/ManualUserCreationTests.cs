using Assignment1_Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace RagEdu.Tests.Services;

public sealed class ManualUserCreationTests
{
    [Fact]
    public async Task CreateUserAsync_GeneratesStudentIdentityAndDefaultPassword()
    {
        await using var context = CreateContext();
        var notification = new RecordingAccountNotificationService();
        var service = CreateService(context, notification);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "  Nguyễn   Minh An  ",
            RoleId = 3
        });

        Assert.True(result.Success);
        Assert.Equal("Nguyễn Minh An", result.FullName);
        Assert.Equal("nguyenminhan", result.Username);
        Assert.Equal("nguyenminhan@fpt.edu.vn", result.Email);
        Assert.Equal(UserServices.DefaultPassword, result.TemporaryPassword);

        var user = Assert.Single(await context.Users.ToListAsync());
        Assert.Equal(result.Username, user.Username);
        Assert.Equal(result.Email, user.Email);
        Assert.Equal(UserServices.DefaultPassword, user.Password);
        Assert.Equal(3, user.RoleId);
        Assert.Null(user.SubjectId);
        Assert.True(user.IsActive);

        var message = Assert.Single(notification.Messages);
        Assert.Equal(result.Email, message.Email);
        Assert.Equal(result.Username, message.Username);
        Assert.Equal(UserServices.DefaultPassword, message.TemporaryPassword);
    }

    [Fact]
    public async Task CreateUserAsync_AcceptsAdministratorProvidedUsernameAndEmail()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Trần Thị Bình",
            Username = "  Binh.Tran_01  ",
            Email = "  BINH.TRAN@EXAMPLE.EDU.VN ",
            RoleId = 3
        });

        Assert.True(result.Success);
        Assert.Equal("binh.tran_01", result.Username);
        Assert.Equal("binh.tran@example.edu.vn", result.Email);
        var user = Assert.Single(await context.Users.ToListAsync());
        Assert.Equal("binh.tran_01", user.Username);
        Assert.Equal("binh.tran@example.edu.vn", user.Email);
    }

    [Fact]
    public async Task CreateUserAsync_GeneratesRoleSpecificEmailFromProvidedUsername()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var student = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Sinh Viên A",
            Username = "student.a",
            RoleId = 3
        });
        var lecturer = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Giảng Viên B",
            Username = "lecturer.b",
            RoleId = 2
        });

        Assert.Equal("student.a@fpt.edu.vn", student.Email);
        Assert.Equal("lecturer.b@edu.vn", lecturer.Email);
    }

    [Fact]
    public async Task CreateUserAsync_AppendsSuffixWhenGeneratedUsernameExists()
    {
        await using var context = CreateContext();
        context.Users.Add(CreateExistingUser(
            "nguyenminhan",
            "nguyenminhan@fpt.edu.vn"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Nguyễn Minh An",
            RoleId = 3
        });

        Assert.True(result.Success);
        Assert.Equal("nguyenminhan1", result.Username);
        Assert.Equal("nguyenminhan1@fpt.edu.vn", result.Email);
    }

    [Fact]
    public async Task CreateUserAsync_RejectsDuplicateProvidedUsername()
    {
        await using var context = CreateContext();
        context.Users.Add(CreateExistingUser("student.one", "student.one@example.edu.vn"));
        await context.SaveChangesAsync();
        var service = CreateService(context);
        Assert.NotNull(
            await new UserRepository(context).GetByUsernameAsync("student.one"));

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Sinh Viên Mới",
            Username = "STUDENT.ONE",
            Email = "new.student@example.edu.vn",
            RoleId = 3
        });

        Assert.False(result.Success);
        Assert.Contains("đăng nhập", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await context.Users.ToListAsync());
    }

    [Fact]
    public async Task CreateUserAsync_RejectsDuplicateEmailIgnoringCase()
    {
        await using var context = CreateContext();
        context.Users.Add(CreateExistingUser("student.one", "student.one@example.edu.vn"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Sinh Viên Mới",
            Username = "student.two",
            Email = "STUDENT.ONE@EXAMPLE.EDU.VN",
            RoleId = 3
        });

        Assert.False(result.Success);
        Assert.Contains("email", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await context.Users.ToListAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateUserAsync_RejectsBlankFullName(string fullName)
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = fullName,
            RoleId = 3
        });

        Assert.False(result.Success);
        Assert.Empty(await context.Users.ToListAsync());
    }

    [Fact]
    public async Task CreateUserAsync_RejectsFullNameLongerThanDatabaseLimit()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = new string('A', 201),
            RoleId = 3
        });

        Assert.False(result.Success);
        Assert.Contains("200", result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(99)]
    public async Task CreateUserAsync_RejectsUnsupportedRole(int roleId)
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Người Dùng",
            RoleId = roleId
        });

        Assert.False(result.Success);
        Assert.Empty(await context.Users.ToListAsync());
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("username with spaces")]
    [InlineData("tênđăngnhập")]
    [InlineData("user@email")]
    [InlineData("user/name")]
    public async Task CreateUserAsync_RejectsInvalidProvidedUsername(string username)
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Người Dùng",
            Username = username,
            RoleId = 3
        });

        Assert.False(result.Success);
        Assert.Empty(await context.Users.ToListAsync());
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-user.test")]
    [InlineData("email with spaces@example.test")]
    public async Task CreateUserAsync_RejectsInvalidEmail(string email)
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Người Dùng",
            Username = "valid.username",
            Email = email,
            RoleId = 3
        });

        Assert.False(result.Success);
        Assert.Empty(await context.Users.ToListAsync());
    }

    [Fact]
    public async Task CreateUserAsync_IgnoresSubjectForStudent()
    {
        await using var context = CreateContext();
        var subject = AddSubject(context, "PRN222");
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Sinh Viên",
            RoleId = 3,
            SubjectId = subject.Id
        });

        Assert.True(result.Success);
        Assert.Null((await context.Users.SingleAsync()).SubjectId);
        Assert.Null((await context.Subjects.FindAsync(subject.Id))!.LecturerId);
    }

    [Fact]
    public async Task CreateUserAsync_AssignsSelectedSubjectToLecturer()
    {
        await using var context = CreateContext();
        var subject = AddSubject(context, "PRN222");
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Giảng Viên Nguyễn An",
            RoleId = 2,
            SubjectId = subject.Id
        });

        Assert.True(result.Success);
        var lecturer = await context.Users.SingleAsync();
        Assert.Equal(2, lecturer.RoleId);
        Assert.Equal(subject.Id, lecturer.SubjectId);
        Assert.Equal(lecturer.Id, (await context.Subjects.FindAsync(subject.Id))!.LecturerId);
    }

    [Fact]
    public async Task CreateUserAsync_AllowsLecturerWithoutInitialSubject()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Giảng Viên Chưa Phân Công",
            RoleId = 2,
            SubjectId = null
        });

        Assert.True(result.Success);
        Assert.Null((await context.Users.SingleAsync()).SubjectId);
    }

    [Fact]
    public async Task CreateUserAsync_RejectsUnknownSubject()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Giảng Viên",
            RoleId = 2,
            SubjectId = 999
        });

        Assert.False(result.Success);
        Assert.Contains("không tồn tại", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await context.Users.ToListAsync());
    }

    [Fact]
    public async Task CreateUserAsync_RejectsSubjectAssignedToAnotherLecturer()
    {
        await using var context = CreateContext();
        var existingLecturer = CreateExistingUser(
            "lecturer.old",
            "lecturer.old@edu.vn",
            roleId: 2);
        context.Users.Add(existingLecturer);
        var subject = AddSubject(context, "PRN222");
        subject.Lecturer = existingLecturer;
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Giảng Viên Mới",
            RoleId = 2,
            SubjectId = subject.Id
        });

        Assert.False(result.Success);
        Assert.Contains("đã được phân công", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await context.Users.ToListAsync());
        Assert.Equal(existingLecturer.Id, (await context.Subjects.FindAsync(subject.Id))!.LecturerId);
    }

    [Fact]
    public async Task CreateUserAsync_StillSucceedsWhenNotificationCannotBeSent()
    {
        await using var context = CreateContext();
        var notification = new RecordingAccountNotificationService
        {
            Result = new AccountNotificationResult
            {
                IsSuccess = false,
                Message = "SMTP chưa được cấu hình."
            }
        };
        var service = CreateService(context, notification);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Sinh Viên",
            RoleId = 3
        });

        Assert.True(result.Success);
        Assert.False(result.NotificationSent);
        Assert.Equal("SMTP chưa được cấu hình.", result.NotificationMessage);
        Assert.Single(await context.Users.ToListAsync());
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsNotificationSuccess()
    {
        await using var context = CreateContext();
        var notification = new RecordingAccountNotificationService
        {
            Result = new AccountNotificationResult
            {
                IsSuccess = true,
                Message = "Đã gửi email thông báo."
            }
        };
        var service = CreateService(context, notification);

        var result = await service.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = "Sinh Viên",
            RoleId = 3
        });

        Assert.True(result.Success);
        Assert.True(result.NotificationSent);
        Assert.Equal("Đã gửi email thông báo.", result.NotificationMessage);
    }

    [Fact]
    public async Task UserRepository_ListsAdministratorsFirstThenNewestUsers()
    {
        await using var context = CreateContext();
        var now = DateTime.Now;
        context.Roles.AddRange(
            new Role { Id = 1, Name = "Admin", Label = "Quản trị viên" },
            new Role { Id = 2, Name = "Lecturer", Label = "Giảng viên" },
            new Role { Id = 3, Name = "Student", Label = "Sinh viên" });
        context.Users.AddRange(
            CreateExistingUser(
                "older.student",
                "older.student@fpt.edu.vn",
                roleId: 3,
                createdAt: now.AddDays(-3)),
            CreateExistingUser(
                "administrator",
                "administrator@edu.vn",
                roleId: 1,
                createdAt: now.AddDays(-10)),
            CreateExistingUser(
                "older.lecturer",
                "older.lecturer@edu.vn",
                roleId: 2,
                createdAt: now.AddDays(-2)),
            CreateExistingUser(
                "new.student",
                "new.student@fpt.edu.vn",
                roleId: 3,
                createdAt: now));
        await context.SaveChangesAsync();

        var users = await new UserRepository(context).GetAllAsync();

        Assert.Equal(
            ["administrator", "new.student", "older.lecturer", "older.student"],
            users.Select(user => user.Username));
    }

    private static RagEduContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RagEduContext>()
            .UseInMemoryDatabase($"manual-user-{Guid.NewGuid():N}")
            .Options;
        return new RagEduContext(options);
    }

    private static UserServices CreateService(
        RagEduContext context,
        RecordingAccountNotificationService? notification = null)
    {
        return new UserServices(
            new UserRepository(context),
            new SubjectRepository(context),
            context,
            notification ?? new RecordingAccountNotificationService());
    }

    private static User CreateExistingUser(
        string username,
        string email,
        int roleId = 3,
        DateTime? createdAt = null)
    {
        return new User
        {
            Username = username,
            Email = email,
            FullName = username,
            Password = "changed-password",
            RoleId = roleId,
            IsActive = true,
            CreatedAt = createdAt ?? DateTime.Now,
            UpdatedAt = createdAt ?? DateTime.Now
        };
    }

    private static Subject AddSubject(RagEduContext context, string code)
    {
        var subject = new Subject
        {
            Code = code,
            Name = $"Môn {code}",
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };
        context.Subjects.Add(subject);
        return subject;
    }

    private sealed class RecordingAccountNotificationService : IAccountNotificationService
    {
        public List<NotificationMessage> Messages { get; } = [];

        public AccountNotificationResult Result { get; set; } = new()
        {
            IsSuccess = true,
            Message = "Đã gửi email thông báo."
        };

        public Task<AccountNotificationResult> SendAccountCreatedEmailAsync(
            string toEmail,
            string fullName,
            string username,
            string temporaryPassword,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(new NotificationMessage(
                toEmail,
                fullName,
                username,
                temporaryPassword));
            return Task.FromResult(Result);
        }

        public Task<AccountNotificationResult> SendDuplicateDocumentNotificationEmailAsync(
            string toEmail,
            string fullName,
            string documentName,
            string subjectCode,
            string subjectName,
            string reason,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result);
        }
    }

    private sealed record NotificationMessage(
        string Email,
        string FullName,
        string Username,
        string TemporaryPassword);
}
