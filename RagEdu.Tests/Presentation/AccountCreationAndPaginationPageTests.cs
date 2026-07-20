using Assignmet1_Presentation.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using RagEdu.Tests.Infrastructure;
using AdminIndexModel = Assignmet1_Presentation.Pages.Admin.IndexModel;
using AuditIndexModel = Assignmet1_Presentation.Pages.Audit.IndexModel;
using ImportUsersModel = Assignmet1_Presentation.Pages.Admin.ImportUsersModel;

namespace RagEdu.Tests.Presentation;

public sealed class AccountCreationAndPaginationPageTests
{
    [Fact]
    public async Task ManualCreation_DoesNotRequireAnImportFile()
    {
        var users = new StubUserService
        {
            CreateResult = new CreateUserResultDto
            {
                Success = false,
                Error = "Lỗi kiểm thử sau khi dịch vụ đã được gọi."
            }
        };
        var model = new ImportUsersModel(users, new StubSubjectService())
        {
            PageContext = CreatePageContext(),
            ManualInput = new ManualCreateUserViewModel
            {
                FullName = "Nguyễn Minh Anh",
                RoleId = 3
            }
        };
        model.ModelState.AddModelError(
            "Input.File",
            "Vui lòng chọn tệp (.xlsx, .csv, .json hoặc .txt).");

        var result = await model.OnPostCreateManualAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(1, users.CreateCallCount);
        Assert.Equal(0, users.ImportCallCount);
        Assert.DoesNotContain(
            model.ModelState.Keys,
            key => key.StartsWith("Input.", StringComparison.Ordinal));
        Assert.NotNull(users.LastCreateRequest);
        Assert.Equal("Nguyễn Minh Anh", users.LastCreateRequest.FullName);
    }

    [Fact]
    public async Task ManualCreation_RedirectsToTheCreatedUserInManagementList()
    {
        var users = new StubUserService
        {
            CreateResult = new CreateUserResultDto
            {
                Success = true,
                UserId = 91,
                FullName = "Nguyễn Công Lập",
                Username = "nguyenconglap",
                Email = "nguyenconglap@edu.vn",
                TemporaryPassword = "1234567",
                NotificationSent = true
            }
        };
        var pageContext = CreatePageContext();
        var model = new ImportUsersModel(users, new StubSubjectService())
        {
            PageContext = pageContext,
            TempData = new TempDataDictionary(
                pageContext.HttpContext,
                new TestTempDataProvider()),
            ManualInput = new ManualCreateUserViewModel
            {
                FullName = "Nguyễn Công Lập",
                RoleId = 2
            }
        };

        var result = Assert.IsType<RedirectToPageResult>(
            await model.OnPostCreateManualAsync());

        Assert.Equal("/Admin/Index", result.PageName);
        Assert.NotNull(result.RouteValues);
        Assert.Equal(91, result.RouteValues["createdUserId"]);
        Assert.Equal(1, users.CreateCallCount);
        Assert.Equal(0, users.ImportCallCount);
    }

    [Fact]
    public async Task BulkImport_StillRequiresAFile()
    {
        var users = new StubUserService();
        var model = new ImportUsersModel(users, new StubSubjectService())
        {
            PageContext = CreatePageContext(),
            Input = new ImportUsersViewModel
            {
                RoleId = 3,
                File = null
            }
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Contains(
            model.ModelState["Input.File"]!.Errors,
            error => error.ErrorMessage.Contains(
                "chọn tệp",
                StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, users.ImportCallCount);
        Assert.Equal(0, users.CreateCallCount);
    }

    [Fact]
    public async Task UserList_SecondPageReturnsTheNextTenUsers()
    {
        var users = new StubUserService
        {
            Users = Enumerable.Range(1, 25)
                .Select(id => CreateUser(id))
                .ToList()
        };
        var model = new AdminIndexModel(users, new StubSubjectService())
        {
            PageNumber = 2
        };

        await model.OnGetAsync();

        Assert.Equal(2, model.ViewModel.CurrentPage);
        Assert.Equal(3, model.ViewModel.TotalPages);
        Assert.Equal(25, model.ViewModel.TotalItems);
        Assert.Equal(25, model.ViewModel.ActiveItems);
        Assert.Equal(0, model.ViewModel.LecturerItems);
        Assert.Equal(25, model.ViewModel.StudentItems);
        Assert.True(model.ViewModel.HasPreviousPage);
        Assert.True(model.ViewModel.HasNextPage);
        Assert.Equal(
            Enumerable.Range(11, 10),
            model.ViewModel.Users.Select(user => user.Id));
    }

    [Theory]
    [InlineData(-5, 1, 1)]
    [InlineData(0, 1, 1)]
    [InlineData(99, 3, 21)]
    public async Task UserList_ClampsInvalidPageNumbers(
        int requestedPage,
        int expectedPage,
        int expectedFirstUserId)
    {
        var users = new StubUserService
        {
            Users = Enumerable.Range(1, 25)
                .Select(id => CreateUser(id))
                .ToList()
        };
        var model = new AdminIndexModel(users, new StubSubjectService())
        {
            PageNumber = requestedPage
        };

        await model.OnGetAsync();

        Assert.Equal(expectedPage, model.PageNumber);
        Assert.Equal(expectedPage, model.ViewModel.CurrentPage);
        Assert.Equal(expectedFirstUserId, model.ViewModel.Users[0].Id);
    }

    [Fact]
    public async Task UserList_PreservesFiltersWhenLoadingNextPage()
    {
        var users = new StubUserService
        {
            Users = Enumerable.Range(1, 30)
                .Select(id => CreateUser(
                    id,
                    roleId: id <= 25 ? 3 : 2,
                    fullName: $"Sinh viên phân trang {id:00}"))
                .ToList()
        };
        var model = new AdminIndexModel(users, new StubSubjectService())
        {
            Search = "phân trang",
            RoleId = 3,
            PageNumber = 2
        };

        await model.OnGetAsync();

        Assert.Equal("phân trang", model.ViewModel.SearchTerm);
        Assert.Equal(3, model.ViewModel.FilterRoleId);
        Assert.Equal(2, model.ViewModel.CurrentPage);
        Assert.Equal(10, model.ViewModel.Users.Count);
        Assert.All(model.ViewModel.Users, user => Assert.Equal(3, user.RoleId));
    }

    [Fact]
    public async Task AuditLog_RequestsSelectedPageAndPreservesFilters()
    {
        var audit = new CapturingAuditLogService
        {
            Result = new AuditLogPageDto
            {
                CurrentPage = 2,
                TotalPages = 4,
                PageSize = 25,
                TotalItems = 80
            }
        };
        var model = new AuditIndexModel(audit)
        {
            PageContext = CreatePageContext(userId: 9, roleId: 1),
            Category = "users",
            Action = "create",
            Search = "Nguyễn",
            FromDate = new DateTime(2026, 7, 1),
            ToDate = new DateTime(2026, 7, 20),
            PageNumber = 2
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.NotNull(audit.LastQuery);
        Assert.Equal(2, audit.LastQuery.Page);
        Assert.Equal("users", audit.LastQuery.Category);
        Assert.Equal("create", audit.LastQuery.Action);
        Assert.Equal("Nguyễn", audit.LastQuery.Search);
        Assert.Equal(2, model.PageNumber);
        Assert.True(model.Data.HasPreviousPage);
        Assert.True(model.Data.HasNextPage);
    }

    private static UserListItemDto CreateUser(
        int id,
        int roleId = 3,
        string? fullName = null)
    {
        return new UserListItemDto
        {
            Id = id,
            Username = $"user{id:00}",
            Email = $"user{id:00}@fpt.edu.vn",
            FullName = fullName ?? $"Người dùng {id:00}",
            RoleId = roleId,
            RoleName = roleId == 2 ? "Lecturer" : "Student",
            RoleLabel = roleId == 2 ? "Giảng viên" : "Sinh viên",
            IsActive = true
        };
    }

    private static PageContext CreatePageContext(
        int? userId = null,
        int? roleId = null)
    {
        var httpContext = new DefaultHttpContext();
        var session = new TestSession();
        httpContext.Features.Set<ISessionFeature>(
            new TestSessionFeature { Session = session });
        if (userId.HasValue)
            session.SetInt32("UserId", userId.Value);
        if (roleId.HasValue)
            session.SetInt32("RoleId", roleId.Value);

        return new PageContext(new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor()));
    }

    private sealed class TestSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
            => new Dictionary<string, object>();

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }

    private sealed class StubUserService : IUserServices
    {
        public List<UserListItemDto> Users { get; init; } = [];
        public CreateUserResultDto CreateResult { get; init; } = new();
        public int CreateCallCount { get; private set; }
        public int ImportCallCount { get; private set; }
        public CreateUserRequestDto? LastCreateRequest { get; private set; }

        public Task<LoginUserDto?> LoginAsync(string username, string password)
            => Task.FromResult<LoginUserDto?>(null);

        public bool IsDefaultPassword(string password) => false;

        public Task<(bool Success, string? Error)> ChangeDefaultPasswordAsync(
            int userId,
            string newPassword)
            => Task.FromResult<(bool, string?)>((true, null));

        public Task<List<UserListItemDto>> GetAllUsersAsync()
            => Task.FromResult(Users);

        public Task<CreateUserResultDto> CreateUserAsync(
            CreateUserRequestDto request)
        {
            CreateCallCount++;
            LastCreateRequest = request;
            return Task.FromResult(CreateResult);
        }

        public Task<ImportUsersResultDto> ImportUsersFromFileAsync(
            Stream stream,
            string fileName,
            int? subjectId,
            int roleId)
        {
            ImportCallCount++;
            return Task.FromResult(new ImportUsersResultDto());
        }

        public Task<(bool Success, string? Error)> AssignSubjectAsync(
            int userId,
            int? subjectId)
            => Task.FromResult<(bool, string?)>((true, null));

        public Task<(bool Success, string? Error)> UpdateLecturerSubjectsAsync(
            int userId,
            IReadOnlyCollection<int> subjectIds)
            => Task.FromResult<(bool, string?)>((true, null));

        public Task<(bool IsAvailable, string? Error)>
            ValidateTeacherSubjectAvailabilityAsync(
                int subjectId,
                int? excludeUserId = null)
            => Task.FromResult<(bool, string?)>((true, null));

        public Task<(bool Success, string? Error)> ToggleUserStatusAsync(int userId)
            => Task.FromResult<(bool, string?)>((true, null));

        public Task<(bool Success, string? Error)> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword)
            => Task.FromResult<(bool, string?)>((true, null));
    }

    private sealed class StubSubjectService : ISubjectService
    {
        public Task<List<SubjectListItemDto>> GetSubjectsAsync()
            => Task.FromResult(new List<SubjectListItemDto>());

        public Task<SubjectDetailDto?> GetSubjectAsync(int id)
            => Task.FromResult<SubjectDetailDto?>(null);

        public Task<SubjectDetailDto> CreateSubjectAsync(
            string code,
            string name,
            string? description = null)
            => throw new NotSupportedException();

        public Task<SubjectDetailDto> UpdateSubjectAsync(
            int id,
            string code,
            string name,
            string? description = null)
            => throw new NotSupportedException();

        public Task<(bool Success, string? Error)> DeleteSubjectAsync(int id)
            => Task.FromResult<(bool, string?)>((true, null));

        public Task<(bool Success, string? Error)> DeleteSubjectWithDocumentsAsync(
            int id,
            int? deletedByUserId = null)
            => Task.FromResult<(bool, string?)>((true, null));

        public Task<List<SubjectListItemDto>> GetDeletedSubjectsAsync()
            => Task.FromResult(new List<SubjectListItemDto>());

        public Task<bool> RestoreSubjectAsync(int id)
            => Task.FromResult(true);
    }

    private sealed class CapturingAuditLogService : IAuditLogService
    {
        public AuditLogPageDto Result { get; init; } = new();
        public AuditLogQuery? LastQuery { get; private set; }

        public Task RecordAsync(
            RecordAuditLogRequest request,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<AuditLogPageDto> GetPageAsync(
            int requesterUserId,
            int requesterRoleId,
            AuditLogQuery query,
            CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            return Task.FromResult(Result);
        }
    }
}
