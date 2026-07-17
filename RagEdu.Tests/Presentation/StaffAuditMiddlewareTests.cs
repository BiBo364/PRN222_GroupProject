using Assignmet1_Presentation.Infrastructure;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using RagEdu.Tests.Fakes;
using RagEdu.Tests.Infrastructure;

namespace RagEdu.Tests.Presentation;

public sealed class StaffAuditMiddlewareTests
{
    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task InvokeAsync_DoesNotAuditReadOnlyRequests(string method)
    {
        var context = CreateContext(method, "/Learning/Index", userId: 10, roleId: 2);
        var audit = new FakeAuditLogService();
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, audit);

        Assert.True(nextCalled);
        Assert.Empty(audit.RecordedRequests);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task InvokeAsync_AuditsMutatingRequests(string method)
    {
        var context = CreateContext(method, "/Learning/ManualQuiz", userId: 10, roleId: 2);
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, audit);

        var request = Assert.Single(audit.RecordedRequests);
        Assert.Equal(method, request.HttpMethod);
        Assert.Equal("/Learning/ManualQuiz", request.RequestPath);
        Assert.Equal(10, request.UserId);
        Assert.Equal(2, request.RoleId);
        Assert.Equal(200, request.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotAuditAnonymousRequest()
    {
        var context = CreateContext("POST", "/Learning/ManualQuiz");
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, audit);

        Assert.Empty(audit.RecordedRequests);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(99)]
    public async Task InvokeAsync_DoesNotAuditNonStaffRole(int roleId)
    {
        var context = CreateContext(
            "POST",
            "/Learning/ManualQuiz",
            userId: 10,
            roleId: roleId);
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, audit);

        Assert.Empty(audit.RecordedRequests);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task InvokeAsync_AuditsAdministratorAndLecturer(int roleId)
    {
        var context = CreateContext(
            "POST",
            "/Learning/ManualQuiz",
            userId: 10,
            roleId: roleId);
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, audit);

        Assert.Equal(roleId, audit.RecordedRequests.Single().RoleId);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/HEALTH/ready")]
    public async Task InvokeAsync_DoesNotAuditHealthEndpoints(string path)
    {
        var context = CreateContext("POST", path, userId: 10, roleId: 1);
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, audit);

        Assert.Empty(audit.RecordedRequests);
    }

    [Theory]
    [InlineData("Autosave")]
    [InlineData("autosave")]
    [InlineData("AUTOSAVE")]
    public async Task InvokeAsync_DoesNotAuditFrequentAutosave(string handler)
    {
        var context = CreateContext("POST", "/Learning/ManualQuiz", userId: 10, roleId: 2);
        context.Request.QueryString = new QueryString($"?handler={handler}");
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, audit);

        Assert.Empty(audit.RecordedRequests);
    }

    [Theory]
    [MemberData(nameof(CategoryCases))]
    public async Task InvokeAsync_ClassifiesCategoryAndEntityType(
        string path,
        string expectedCategory,
        string expectedEntityType)
    {
        var context = CreateContext("POST", path, userId: 10, roleId: 2);
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, audit);

        var request = Assert.Single(audit.RecordedRequests);
        Assert.Equal(expectedCategory, request.Category);
        Assert.Equal(expectedEntityType, request.EntityType);
    }

    public static TheoryData<string, string, string> CategoryCases => new()
    {
        { "/Learning/ManualQuiz", "learning", "learning_set" },
        { "/Documents/Upload", "documents", "document" },
        { "/Subjects/Edit", "subjects", "subject" },
        { "/Admin/Users", "users", "user" },
        { "/PaymentAdmin/Index", "payments", "payment" },
        { "/Account/ChangePassword", "account", "account" },
        { "/Home/Index", "system", "request" }
    };

    [Theory]
    [MemberData(nameof(ActionCases))]
    public async Task InvokeAsync_ClassifiesAction(
        string path,
        string handler,
        string expectedAction)
    {
        var context = CreateContext("POST", path, userId: 10, roleId: 2);
        if (!string.IsNullOrEmpty(handler))
            context.Request.QueryString = new QueryString($"?handler={handler}");
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, audit);

        Assert.Equal(expectedAction, audit.RecordedRequests.Single().Action);
    }

    public static TheoryData<string, string, string> ActionCases => new()
    {
        { "/Learning/Index", "Delete", "delete" },
        { "/Learning/Index", "PermanentlyDelete", "delete" },
        { "/Learning/RecycleBin", "Restore", "restore" },
        { "/Learning/Index", "Publish", "publish" },
        { "/Documents/Import", "", "import" },
        { "/Documents/Index", "ImportCsv", "import" },
        { "/PaymentAdmin/Index", "", "review" },
        { "/Learning/Create", "", "create" },
        { "/Learning/Generate", "", "create" },
        { "/Learning/Compose", "", "create" },
        { "/Learning/ManualQuiz", "Save", "update" }
    };

    [Fact]
    public async Task InvokeAsync_ClassifiesUnpublishFromFormValue()
    {
        var context = CreateContext("POST", "/Learning/Index", userId: 10, roleId: 2);
        context.Request.QueryString = new QueryString("?handler=Publish");
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["isPublished"] = "false"
        });
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, audit);

        Assert.Equal("unpublish", audit.RecordedRequests.Single().Action);
    }

    [Fact]
    public async Task InvokeAsync_CapturesRouteEntityId()
    {
        var context = CreateContext("DELETE", "/Learning/Quiz/42", userId: 10, roleId: 2);
        context.Request.RouteValues["id"] = 42;
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, audit);

        Assert.Equal("42", audit.RecordedRequests.Single().EntityId);
    }

    [Fact]
    public async Task InvokeAsync_FallsBackToQueryEntityId()
    {
        var context = CreateContext("DELETE", "/Learning/Quiz", userId: 10, roleId: 2);
        context.Request.QueryString = new QueryString("?id=73&handler=Delete");
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, audit);

        Assert.Equal("73", audit.RecordedRequests.Single().EntityId);
    }

    [Fact]
    public async Task InvokeAsync_CapturesStatusCodeFromNextMiddleware()
    {
        var context = CreateContext("POST", "/Learning/ManualQuiz", userId: 10, roleId: 2);
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(httpContext =>
        {
            httpContext.Response.StatusCode = StatusCodes.Status201Created;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, audit);

        Assert.Equal(201, audit.RecordedRequests.Single().StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_RecordsServerErrorAndRethrowsException()
    {
        var context = CreateContext("POST", "/Learning/ManualQuiz", userId: 10, roleId: 2);
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(_ =>
            throw new InvalidOperationException("next failed"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context, audit));

        Assert.Equal("next failed", exception.Message);
        Assert.Equal(500, audit.RecordedRequests.Single().StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotBreakRequestWhenAuditStorageFails()
    {
        var context = CreateContext("POST", "/Learning/ManualQuiz", userId: 10, roleId: 2);
        var audit = new FakeAuditLogService
        {
            RecordException = new InvalidOperationException("audit database unavailable")
        };
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, audit);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_CapturesRequestMetadataAndTraceIdentifier()
    {
        var context = CreateContext("PATCH", "/Subjects/Edit", userId: 30, roleId: 1);
        context.TraceIdentifier = "trace-audit-123";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        context.Request.Headers.UserAgent = "RagEdu.Tests/1.0";
        var audit = new FakeAuditLogService();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, audit);

        var request = Assert.Single(audit.RecordedRequests);
        Assert.Equal("127.0.0.1", request.IpAddress);
        Assert.Equal("RagEdu.Tests/1.0", request.UserAgent);
        Assert.Equal("trace-audit-123", request.TraceIdentifier);
        Assert.NotNull(request.Details);
    }

    private static StaffAuditMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new StaffAuditMiddleware(
            next,
            NullLogger<StaffAuditMiddleware>.Instance);
    }

    private static DefaultHttpContext CreateContext(
        string method,
        string path,
        int? userId = null,
        int? roleId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        var session = new TestSession();
        context.Features.Set<ISessionFeature>(new TestSessionFeature
        {
            Session = session
        });
        if (userId.HasValue)
            session.SetInt32("UserId", userId.Value);
        if (roleId.HasValue)
            session.SetInt32("RoleId", roleId.Value);
        return context;
    }

    private sealed class TestSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }
}
