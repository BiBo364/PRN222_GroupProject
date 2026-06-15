using Assignmet1_Presentation.Endpoints;
using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Hubs;
using Assignment1_Service;
using Assignment1_Service.Models;
using Assignment1_Service.Infrastructure;
using Assignment1_Repository.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.ConfigureFilter(new EnforcePasswordChangeAttribute());
    options.Conventions.AddPageRoute("/Account/Login", "");
});
builder.Services.AddSignalR();
builder.Services.AddAssignment1Services(builder.Configuration);
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<MoMoPaymentSettings>(builder.Configuration.GetSection(MoMoPaymentSettings.SectionName));
builder.Services.Configure<SubscriptionQuotaSettings>(builder.Configuration.GetSection(SubscriptionQuotaSettings.SectionName));

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
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
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
            "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "connect-src 'self' wss: https://cdnjs.cloudflare.com;"
        );
        await next();
    });
}

app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();
app.MapApiEndpoints();
app.MapHub<AppHub>("/hubs/app");
app.Run();