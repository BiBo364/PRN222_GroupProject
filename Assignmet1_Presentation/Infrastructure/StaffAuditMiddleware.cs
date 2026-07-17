using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;

namespace Assignmet1_Presentation.Infrastructure;

public sealed class StaffAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<StaffAuditMiddleware> _logger;

    public StaffAuditMiddleware(
        RequestDelegate next,
        ILogger<StaffAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuditLogService auditLogService)
    {
        var shouldAudit = ShouldAudit(context);
        var userId = context.Session.GetInt32("UserId");
        var roleId = context.Session.GetInt32("RoleId");
        if (!shouldAudit || userId is null || roleId is not 1 and not 2)
        {
            await _next(context);
            return;
        }

        var startedAt = DateTime.UtcNow;
        var statusCode = StatusCodes.Status200OK;
        try
        {
            await _next(context);
            statusCode = context.Response.StatusCode;
        }
        catch
        {
            statusCode = StatusCodes.Status500InternalServerError;
            throw;
        }
        finally
        {
            try
            {
                var descriptor = Describe(context);
                await auditLogService.RecordAsync(
                    new RecordAuditLogRequest
                    {
                        UserId = userId,
                        RoleId = roleId,
                        Action = descriptor.Action,
                        Category = descriptor.Category,
                        EntityType = descriptor.EntityType,
                        EntityId = descriptor.EntityId,
                        Description = descriptor.Description,
                        Details = new
                        {
                            durationMs = Math.Max(
                                0,
                                (long)(DateTime.UtcNow - startedAt).TotalMilliseconds),
                            queryHandler = context.Request.Query["handler"].ToString()
                        },
                        IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = context.Request.Headers.UserAgent.ToString(),
                        RequestPath = context.Request.Path.Value,
                        HttpMethod = context.Request.Method,
                        StatusCode = statusCode,
                        TraceIdentifier = context.TraceIdentifier
                    },
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể ghi nhật ký cho yêu cầu {Method} {Path}.",
                    context.Request.Method,
                    context.Request.Path);
            }
        }
    }

    private static bool ShouldAudit(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method)
            && !HttpMethods.IsPut(context.Request.Method)
            && !HttpMethods.IsPatch(context.Request.Method)
            && !HttpMethods.IsDelete(context.Request.Method))
        {
            return false;
        }

        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
            return false;

        return !string.Equals(
            context.Request.Query["handler"].ToString(),
            "Autosave",
            StringComparison.OrdinalIgnoreCase);
    }

    private static AuditDescriptor Describe(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        var handler = context.Request.Query["handler"].ToString();
        var category = Category(path);
        var entityType = EntityType(category);
        var action = Action(context, handler, path);
        var actionLabel = action switch
        {
            "create" => "Tạo mới",
            "delete" => "Xóa",
            "restore" => "Khôi phục",
            "publish" => "Phát hành",
            "unpublish" => "Thu hồi",
            "import" => "Nhập dữ liệu",
            "review" => "Duyệt",
            _ => "Cập nhật"
        };
        var entityLabel = category switch
        {
            "learning" => "Quiz hoặc nội dung ôn tập",
            "documents" => "tài liệu",
            "subjects" => "môn học",
            "users" => "người dùng",
            "payments" => "giao dịch",
            "account" => "tài khoản",
            _ => "dữ liệu hệ thống"
        };
        var entityId = context.Request.RouteValues.TryGetValue("id", out var routeId)
            ? routeId?.ToString()
            : context.Request.Query["id"].FirstOrDefault();

        return new AuditDescriptor(
            action,
            category,
            entityType,
            entityId,
            $"{actionLabel} {entityLabel}.");
    }

    private static string Category(string path)
    {
        if (path.StartsWith("/Learning", StringComparison.OrdinalIgnoreCase))
            return "learning";
        if (path.StartsWith("/Documents", StringComparison.OrdinalIgnoreCase))
            return "documents";
        if (path.StartsWith("/Subjects", StringComparison.OrdinalIgnoreCase))
            return "subjects";
        if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
            return "users";
        if (path.StartsWith("/PaymentAdmin", StringComparison.OrdinalIgnoreCase))
            return "payments";
        if (path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase))
            return "account";
        return "system";
    }

    private static string EntityType(string category) => category switch
    {
        "learning" => "learning_set",
        "documents" => "document",
        "subjects" => "subject",
        "users" => "user",
        "payments" => "payment",
        "account" => "account",
        _ => "request"
    };

    private static string Action(HttpContext context, string handler, string path)
    {
        if (handler.Contains("PermanentlyDelete", StringComparison.OrdinalIgnoreCase)
            || handler.Contains("Delete", StringComparison.OrdinalIgnoreCase))
        {
            return "delete";
        }
        if (handler.Contains("Restore", StringComparison.OrdinalIgnoreCase))
            return "restore";
        if (handler.Contains("Publish", StringComparison.OrdinalIgnoreCase))
        {
            if (context.Request.HasFormContentType
                && bool.TryParse(context.Request.Form["isPublished"], out var isPublished)
                && !isPublished)
            {
                return "unpublish";
            }

            return "publish";
        }
        if (handler.Contains("Import", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/Import", StringComparison.OrdinalIgnoreCase))
        {
            return "import";
        }
        if (handler.Contains("Review", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/PaymentAdmin", StringComparison.OrdinalIgnoreCase))
        {
            return "review";
        }
        if (path.Contains("/Create", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/Generate", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/Compose", StringComparison.OrdinalIgnoreCase))
        {
            return "create";
        }
        return "update";
    }

    private sealed record AuditDescriptor(
        string Action,
        string Category,
        string EntityType,
        string? EntityId,
        string Description);
}
