using Assignmet1_Presentation.Endpoints;
using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Hubs;
using Assignmet1_Presentation.Infrastructure;
using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service;
using Assignment1_Service.Infrastructure;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.ConfigureFilter(new EnforcePasswordChangeAttribute());
    options.Conventions.AddPageRoute("/Account/Login", "");
});
builder.Services.AddSignalR();
builder.Services.AddAssignment1Services(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services
    .AddHealthChecks()
    .AddCheck("application", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.StartsWithSegments("/Error", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter("system-endpoint");
        }

        var partitionKey = context.Session.GetInt32("UserId") is int userId
            ? $"user:{userId}"
            : $"ip:{context.Connection.RemoteIpAddress}";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 180,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.AddPolicy("ai-generation", context =>
    {
        var key = context.Session.GetInt32("UserId") is int userId
            ? $"ai-user:{userId}"
            : $"ai-ip:{context.Connection.RemoteIpAddress}";
        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.AddPolicy("quiz-submission", context =>
    {
        var key = context.Session.GetInt32("UserId") is int userId
            ? $"quiz-user:{userId}"
            : $"quiz-ip:{context.Connection.RemoteIpAddress}";
        return RateLimitPartition.GetSlidingWindowLimiter(
            key,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.OnRejected = async (rejectionContext, cancellationToken) =>
    {
        var response = rejectionContext.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.Headers.RetryAfter = "60";
        var request = rejectionContext.HttpContext.Request;
        var expectsJson = request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            || request.Headers.Accept.Any(value =>
                value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);

        if (expectsJson)
        {
            await response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Bạn đang gửi quá nhiều yêu cầu",
                    Detail = "Vui lòng chờ một phút trước khi thử lại.",
                    Instance = request.Path
                },
                cancellationToken);
            return;
        }

        response.ContentType = "text/html; charset=utf-8";
        await response.WriteAsync(
            """
            <!doctype html>
            <html lang="vi">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Yêu cầu quá nhanh</title></head>
            <body style="font-family:system-ui;background:#f0f4ff;color:#0f172a;display:grid;place-items:center;min-height:100vh;margin:0">
              <main style="max-width:560px;background:#fff;padding:32px;border-radius:20px;box-shadow:0 20px 50px rgba(99,102,241,.16);text-align:center">
                <h1>Vui lòng thao tác chậm lại</h1>
                <p>Hệ thống đang nhận quá nhiều yêu cầu từ phiên làm việc này. Vui lòng chờ một phút rồi thử lại.</p>
                <a href="/" style="display:inline-block;margin-top:12px;padding:11px 18px;border-radius:10px;background:#4f46e5;color:#fff;text-decoration:none">Về trang chủ</a>
              </main>
            </body>
            </html>
            """,
            cancellationToken);
    };
});
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<MoMoPaymentSettings>(builder.Configuration.GetSection(MoMoPaymentSettings.SectionName));
builder.Services.Configure<SubscriptionQuotaSettings>(builder.Configuration.GetSection(SubscriptionQuotaSettings.SectionName));
builder.Services.Configure<ChunkingSettings>(builder.Configuration.GetSection(ChunkingSettings.SectionName));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RagEduContext>();
    await DatabaseSchemaSynchronizer.UpdateAsync(db);

    // Khởi tạo cấu hình chunk mặc định từ appsettings.json nếu DB chưa có bản ghi.
    // Các thay đổi do Admin lưu trong DB được giữ nguyên sau khi restart.
    var docRepo        = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
    var chunkSettings  = scope.ServiceProvider.GetRequiredService<IOptions<ChunkingSettings>>().Value;
    var startupLogger  = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await ChunkingConfigSynchronizer.SyncAsync(docRepo, chunkSettings, startupLogger);
}

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
if (!app.Environment.IsDevelopment())
{
    app.Use(async (ctx, next) =>
    {
        ctx.Response.Headers.Append(
            "Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://unpkg.com https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
            "font-src 'self' data: https://cdnjs.cloudflare.com https://fonts.gstatic.com; " +
            "img-src 'self' data:; " +
            "connect-src 'self' wss: https://cdnjs.cloudflare.com https://unpkg.com https://cdn.jsdelivr.net;"
        );
        await next();
    });
}

app.UseRouting();
app.UseSession();
app.UseRateLimiter();
app.UseMiddleware<StaffAuditMiddleware>();
app.UseAuthorization();
app.MapPost("/MoMoPayment/Ipn", async (HttpRequest request, IMomoPaymentService momoPaymentService) =>
{
    var callback = await request.ReadFromJsonAsync<MoMoCallbackRequestDto>();
    if (callback is null)
        return Results.BadRequest(new { message = "Nội dung IPN là bắt buộc." });

    var (success, error) = await momoPaymentService.HandleIpnAsync(callback);
    return success
        ? Results.NoContent()
        : Results.BadRequest(new { message = error ?? "Xử lý IPN thất bại." });
}).DisableAntiforgery();
app.MapRazorPages();
app.MapApiEndpoints();
app.MapHub<AppHub>("/hubs/app");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealthResponseAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteHealthResponseAsync
});
app.Run();

static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";
    return context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString().ToLowerInvariant(),
        durationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
        checks = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => new
            {
                status = entry.Value.Status.ToString().ToLowerInvariant(),
                description = entry.Value.Description,
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1)
            })
    });
}
