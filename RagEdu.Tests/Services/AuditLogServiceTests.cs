using System.Text.Json;
using RagEdu.Tests.Fakes;

namespace RagEdu.Tests.Services;

public sealed class AuditLogServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(99)]
    public async Task RecordAsync_IgnoresRolesWithoutAuditPermission(int? roleId)
    {
        var repository = new FakeAuditLogRepository();
        var service = new AuditLogService(repository);

        await service.RecordAsync(new RecordAuditLogRequest
        {
            UserId = 10,
            RoleId = roleId,
            Action = "update",
            Category = "learning",
            EntityType = "learning_set",
            Description = "Thay đổi Quiz."
        });

        Assert.Empty(repository.Logs);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task RecordAsync_StoresAdministratorAndLecturerActions(int roleId)
    {
        var repository = new FakeAuditLogRepository();
        var service = new AuditLogService(repository);

        await service.RecordAsync(new RecordAuditLogRequest
        {
            UserId = 10,
            RoleId = roleId,
            Action = "publish",
            Category = "learning",
            EntityType = "learning_set",
            EntityId = "42",
            Description = "Phát hành Quiz.",
            StatusCode = 200
        });

        var log = Assert.Single(repository.Logs);
        Assert.Equal(roleId, log.RoleId);
        Assert.Equal("publish", log.Action);
        Assert.Equal("learning", log.Category);
        Assert.Equal("learning_set", log.EntityType);
        Assert.Equal("42", log.EntityId);
        Assert.Equal(200, log.StatusCode);
        Assert.NotEqual(default, log.CreatedAt);
    }

    [Fact]
    public async Task RecordAsync_UsesFallbacksForBlankRequiredFields()
    {
        var repository = new FakeAuditLogRepository();
        var service = new AuditLogService(repository);

        await service.RecordAsync(new RecordAuditLogRequest
        {
            RoleId = 1,
            Action = " ",
            Category = null!,
            EntityType = "",
            Description = "\t"
        });

        var log = Assert.Single(repository.Logs);
        Assert.Equal("update", log.Action);
        Assert.Equal("system", log.Category);
        Assert.Equal("request", log.EntityType);
        Assert.False(string.IsNullOrWhiteSpace(log.Description));
    }

    [Fact]
    public async Task RecordAsync_TrimsAndLimitsAllTextFields()
    {
        var repository = new FakeAuditLogRepository();
        var service = new AuditLogService(repository);

        await service.RecordAsync(new RecordAuditLogRequest
        {
            UserId = 1,
            RoleId = 1,
            Action = "  " + new string('A', 120),
            Category = "  " + new string('C', 120),
            EntityType = "  " + new string('T', 120),
            EntityId = "  " + new string('I', 120),
            Description = "  " + new string('D', 1200),
            IpAddress = "  " + new string('P', 80),
            UserAgent = "  " + new string('U', 600),
            RequestPath = "  /" + new string('R', 1100),
            HttpMethod = "  POST-TOO-LONG",
            TraceIdentifier = "  " + new string('X', 120)
        });

        var log = Assert.Single(repository.Logs);
        Assert.Equal(100, log.Action.Length);
        Assert.Equal(100, log.Category.Length);
        Assert.Equal(100, log.EntityType.Length);
        Assert.Equal(100, log.EntityId!.Length);
        Assert.Equal(1000, log.Description.Length);
        Assert.Equal(64, log.IpAddress!.Length);
        Assert.Equal(500, log.UserAgent!.Length);
        Assert.Equal(1000, log.RequestPath!.Length);
        Assert.Equal(10, log.HttpMethod!.Length);
        Assert.Equal(100, log.TraceIdentifier!.Length);
        Assert.DoesNotContain(" ", log.Action);
    }

    [Fact]
    public async Task RecordAsync_SerializesStructuredDetailsAsJson()
    {
        var repository = new FakeAuditLogRepository();
        var service = new AuditLogService(repository);

        await service.RecordAsync(new RecordAuditLogRequest
        {
            RoleId = 2,
            Action = "update",
            Category = "learning",
            EntityType = "question",
            Description = "Cập nhật câu hỏi.",
            Details = new
            {
                questionId = 15,
                changedFields = new[] { "prompt", "answer" },
                autosave = false
            }
        });

        var log = Assert.Single(repository.Logs);
        using var document = JsonDocument.Parse(log.DetailsJson!);
        Assert.Equal(15, document.RootElement.GetProperty("questionId").GetInt32());
        Assert.Equal(2, document.RootElement.GetProperty("changedFields").GetArrayLength());
        Assert.False(document.RootElement.GetProperty("autosave").GetBoolean());
    }

    [Fact]
    public async Task RecordAsync_LeavesDetailsNullWhenNotProvided()
    {
        var repository = new FakeAuditLogRepository();
        var service = new AuditLogService(repository);

        await service.RecordAsync(new RecordAuditLogRequest
        {
            RoleId = 2,
            Action = "update",
            Category = "learning",
            EntityType = "question",
            Description = "Cập nhật câu hỏi.",
            Details = null
        });

        Assert.Null(repository.Logs.Single().DetailsJson);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(99)]
    public async Task GetPageAsync_RejectsUnauthorizedRole(int roleId)
    {
        var service = new AuditLogService(new FakeAuditLogRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetPageAsync(10, roleId, new AuditLogQuery()));
    }

    [Fact]
    public async Task GetPageAsync_AllowsAdministratorToViewAllUsers()
    {
        var repository = SeedRepository();
        var service = new AuditLogService(repository);

        var result = await service.GetPageAsync(
            requesterUserId: 99,
            requesterRoleId: 1,
            new AuditLogQuery());

        Assert.True(result.CanViewAllUsers);
        Assert.Null(repository.LastUserId);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task GetPageAsync_ScopesLecturerToOwnLogs()
    {
        var repository = SeedRepository();
        var service = new AuditLogService(repository);

        var result = await service.GetPageAsync(
            requesterUserId: 10,
            requesterRoleId: 2,
            new AuditLogQuery());

        Assert.False(result.CanViewAllUsers);
        Assert.Equal(10, repository.LastUserId);
        Assert.Equal(2, result.TotalItems);
        Assert.All(result.Items, item => Assert.Equal(10, item.UserId));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 10)]
    [InlineData(9, 10)]
    [InlineData(10, 10)]
    [InlineData(25, 25)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(1000, 100)]
    public async Task GetPageAsync_ClampsPageSize(int requested, int expected)
    {
        var repository = SeedRepository();
        var service = new AuditLogService(repository);

        var result = await service.GetPageAsync(
            99,
            1,
            new AuditLogQuery { PageSize = requested });

        Assert.Equal(expected, result.PageSize);
        Assert.Equal(expected, repository.LastTake);
    }

    [Theory]
    [InlineData(-10, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(5, 1)]
    public async Task GetPageAsync_NormalizesAndClampsPage(int requestedPage, int expectedPage)
    {
        var repository = SeedRepository();
        var service = new AuditLogService(repository);

        var result = await service.GetPageAsync(
            99,
            1,
            new AuditLogQuery
            {
                Page = requestedPage,
                PageSize = 10
            });

        Assert.Equal(expectedPage, result.CurrentPage);
        Assert.Equal(0, repository.LastSkip);
    }

    [Fact]
    public async Task GetPageAsync_TrimsOptionalFilters()
    {
        var repository = SeedRepository();
        var service = new AuditLogService(repository);

        await service.GetPageAsync(
            99,
            1,
            new AuditLogQuery
            {
                Category = "  learning  ",
                Action = "  publish ",
                Search = "  Quiz "
            });

        Assert.Equal("learning", repository.LastCategory);
        Assert.Equal("publish", repository.LastAction);
        Assert.Equal("Quiz", repository.LastSearch);
    }

    [Fact]
    public async Task GetPageAsync_ConvertsInclusiveDateRangeToExclusiveUtcBoundary()
    {
        var repository = SeedRepository();
        var service = new AuditLogService(repository);
        var from = new DateTime(2026, 7, 1);
        var to = new DateTime(2026, 7, 17);

        await service.GetPageAsync(
            99,
            1,
            new AuditLogQuery
            {
                FromDate = from,
                ToDate = to
            });

        Assert.Equal(from.Date.ToUniversalTime(), repository.LastFromUtc);
        Assert.Equal(to.Date.AddDays(1).ToUniversalTime(), repository.LastToUtc);
    }

    [Theory]
    [InlineData("create", "Tạo mới")]
    [InlineData("update", "Cập nhật")]
    [InlineData("delete", "Xóa")]
    [InlineData("restore", "Khôi phục")]
    [InlineData("publish", "Phát hành")]
    [InlineData("unpublish", "Thu hồi")]
    [InlineData("import", "Nhập dữ liệu")]
    [InlineData("review", "Duyệt")]
    [InlineData("custom", "Thao tác")]
    public async Task GetPageAsync_MapsActionLabels(string action, string expectedLabel)
    {
        var repository = new FakeAuditLogRepository();
        repository.Logs.Add(CreateLog(1, 10, 2, action, "learning"));
        var service = new AuditLogService(repository);

        var result = await service.GetPageAsync(10, 2, new AuditLogQuery());

        Assert.Equal(expectedLabel, result.Items.Single().ActionLabel);
    }

    [Theory]
    [InlineData("learning", "Quiz và ôn tập")]
    [InlineData("documents", "Tài liệu")]
    [InlineData("subjects", "Môn học")]
    [InlineData("users", "Người dùng")]
    [InlineData("payments", "Thanh toán")]
    [InlineData("account", "Tài khoản")]
    [InlineData("custom", "Hệ thống")]
    public async Task GetPageAsync_MapsCategoryLabels(string category, string expectedLabel)
    {
        var repository = new FakeAuditLogRepository();
        repository.Logs.Add(CreateLog(1, 10, 2, "update", category));
        var service = new AuditLogService(repository);

        var result = await service.GetPageAsync(10, 2, new AuditLogQuery());

        Assert.Equal(expectedLabel, result.Items.Single().CategoryLabel);
    }

    [Fact]
    public async Task GetPageAsync_UsesDeletedAccountFallback()
    {
        var repository = new FakeAuditLogRepository();
        repository.Logs.Add(CreateLog(1, 10, 2, "update", "learning", user: null));
        var service = new AuditLogService(repository);

        var result = await service.GetPageAsync(10, 2, new AuditLogQuery());

        Assert.Equal("Tài khoản đã xóa", result.Items.Single().UserDisplayName);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsDistinctVisibleCategories()
    {
        var repository = SeedRepository();
        var service = new AuditLogService(repository);

        var result = await service.GetPageAsync(99, 1, new AuditLogQuery());

        Assert.Equal(["documents", "learning"], result.Categories);
    }

    private static FakeAuditLogRepository SeedRepository()
    {
        var repository = new FakeAuditLogRepository();
        var lecturer = new User
        {
            Id = 10,
            Username = "lecturer",
            Email = "lecturer@example.edu.vn",
            Password = "test",
            FullName = "Giảng viên An",
            RoleId = 2,
            IsActive = true
        };
        var otherLecturer = new User
        {
            Id = 11,
            Username = "other",
            Email = "other@example.edu.vn",
            Password = "test",
            FullName = "Giảng viên Bình",
            RoleId = 2,
            IsActive = true
        };
        repository.Logs.Add(CreateLog(1, 10, 2, "publish", "learning", lecturer));
        repository.Logs.Add(CreateLog(2, 10, 2, "update", "documents", lecturer));
        repository.Logs.Add(CreateLog(3, 11, 2, "delete", "learning", otherLecturer));
        return repository;
    }

    private static AuditLog CreateLog(
        long id,
        int userId,
        int roleId,
        string action,
        string category,
        User? user = null)
    {
        return new AuditLog
        {
            Id = id,
            UserId = userId,
            RoleId = roleId,
            User = user,
            Action = action,
            Category = category,
            EntityType = "learning_set",
            EntityId = id.ToString(),
            Description = $"Thao tác {action} trên Quiz {id}.",
            RequestPath = "/Learning",
            HttpMethod = "POST",
            StatusCode = 200,
            TraceIdentifier = $"trace-{id}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-id)
        };
    }
}
