using System.Text.Json;
using Assignmet1_Presentation.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace RagEdu.Tests.Presentation;

public sealed class GlobalExceptionHandlerTests
{
    [Theory]
    [MemberData(nameof(ExceptionStatusCases))]
    public async Task TryHandleAsync_ReturnsExpectedProblemStatusForJsonRequest(
        Exception exception,
        int expectedStatus)
    {
        var context = CreateContext("/api/learning");
        var handler = CreateHandler();

        var handled = await handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        var problem = await ReadProblemAsync(context);
        Assert.Equal(expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.Equal("/api/learning", problem.GetProperty("instance").GetString());
        Assert.Equal(
            context.TraceIdentifier,
            problem.GetProperty("traceId").GetString());
    }

    public static TheoryData<Exception, int> ExceptionStatusCases => new()
    {
        { new InvalidOperationException("Dữ liệu không hợp lệ."), 400 },
        { new UnauthorizedAccessException("Không có quyền."), 403 },
        { new KeyNotFoundException("Không tìm thấy."), 404 },
        { new Exception("Lỗi bất ngờ."), 500 }
    };

    [Fact]
    public async Task TryHandleAsync_ExposesValidationMessageForBadRequest()
    {
        var context = CreateContext("/api/quiz");
        var handler = CreateHandler();

        await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Quiz phải có ít nhất một câu hỏi."),
            CancellationToken.None);

        var problem = await ReadProblemAsync(context);
        Assert.Equal(
            "Quiz phải có ít nhất một câu hỏi.",
            problem.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("forbidden", 403)]
    [InlineData("not-found", 404)]
    [InlineData("server", 500)]
    public async Task TryHandleAsync_HidesSensitiveMessageForNonValidationErrors(
        string exceptionType,
        int expectedStatus)
    {
        var context = CreateContext("/api/quiz");
        var handler = CreateHandler();
        var exception = exceptionType switch
        {
            "forbidden" => new UnauthorizedAccessException("sensitive-forbidden"),
            "not-found" => new KeyNotFoundException("sensitive-not-found"),
            _ => new Exception("database-password-should-not-leak")
        };

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        var problem = await ReadProblemAsync(context);
        var detail = problem.GetProperty("detail").GetString();
        Assert.Equal(expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.DoesNotContain("sensitive", detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", detail, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/api/quiz")]
    [InlineData("/API/quiz")]
    [InlineData("/MoMoPayment/Ipn")]
    [InlineData("/momopayment/ipn/callback")]
    public async Task TryHandleAsync_ReturnsJsonForApiAndPaymentCallbacks(string path)
    {
        var context = CreateContext(path);
        var handler = CreateHandler();

        await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Sai dữ liệu."),
            CancellationToken.None);

        Assert.Contains(
            "application/json",
            context.Response.ContentType,
            StringComparison.OrdinalIgnoreCase);
        var problem = await ReadProblemAsync(context);
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/json; charset=utf-8")]
    [InlineData("text/html, application/json")]
    [InlineData("APPLICATION/JSON")]
    public async Task TryHandleAsync_ReturnsJsonWhenAcceptHeaderRequestsJson(string accept)
    {
        var context = CreateContext("/Learning/Index");
        context.Request.Headers.Accept = accept;
        var handler = CreateHandler();

        await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Sai dữ liệu."),
            CancellationToken.None);

        Assert.Equal(400, context.Response.StatusCode);
        Assert.Contains("json", context.Response.ContentType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TryHandleAsync_RedirectsBrowserRequestToErrorPage()
    {
        var context = CreateContext("/Learning/Index");
        context.TraceIdentifier = "trace with spaces";
        context.Request.Headers.Accept = "text/html";
        var handler = CreateHandler();

        var handled = await handler.TryHandleAsync(
            context,
            new KeyNotFoundException("Không tìm thấy."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(302, context.Response.StatusCode);
        Assert.Equal(
            "/Error?statusCode=404&requestId=trace%20with%20spaces",
            context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task TryHandleAsync_PreservesRequestPathAndTraceId()
    {
        var context = CreateContext("/api/questions/42");
        context.TraceIdentifier = "trace-42";
        var handler = CreateHandler();

        await handler.TryHandleAsync(
            context,
            new Exception("Lỗi."),
            CancellationToken.None);

        var problem = await ReadProblemAsync(context);
        Assert.Equal("/api/questions/42", problem.GetProperty("instance").GetString());
        Assert.Equal("trace-42", problem.GetProperty("traceId").GetString());
    }

    private static GlobalExceptionHandler CreateHandler()
    {
        return new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = HttpMethods.Get;
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "test-trace";
        return context;
    }

    private static async Task<JsonElement> ReadProblemAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.Clone();
    }
}
