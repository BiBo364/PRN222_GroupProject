using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Assignmet1_Presentation.Infrastructure;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            InvalidOperationException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        _logger.LogError(
            exception,
            "Yêu cầu {Method} {Path} thất bại với mã theo dõi {TraceIdentifier}.",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        if (httpContext.Response.HasStarted)
            return false;

        if (ExpectsJson(httpContext.Request))
        {
            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = statusCode,
                    Title = StatusTitle(statusCode),
                    Detail = statusCode == StatusCodes.Status400BadRequest
                        ? exception.Message
                        : "Yêu cầu chưa thể hoàn tất. Vui lòng thử lại hoặc liên hệ quản trị viên.",
                    Instance = httpContext.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = httpContext.TraceIdentifier
                    }
                },
                cancellationToken);
            return true;
        }

        var target =
            $"/Error?statusCode={statusCode}&requestId={Uri.EscapeDataString(httpContext.TraceIdentifier)}";
        httpContext.Response.Redirect(target);
        return true;
    }

    private static bool ExpectsJson(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            || request.Path.StartsWithSegments("/MoMoPayment/Ipn", StringComparison.OrdinalIgnoreCase)
            || request.Headers.Accept.Any(value =>
                value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
    }

    internal static string StatusTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Yêu cầu không hợp lệ",
        StatusCodes.Status401Unauthorized => "Phiên đăng nhập không hợp lệ",
        StatusCodes.Status403Forbidden => "Bạn không có quyền thực hiện thao tác",
        StatusCodes.Status404NotFound => "Không tìm thấy nội dung",
        StatusCodes.Status429TooManyRequests => "Bạn đang gửi quá nhiều yêu cầu",
        _ => "Đã xảy ra lỗi hệ thống"
    };
}
