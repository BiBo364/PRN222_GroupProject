using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Hubs;
using Assignmet1_Presentation.Models;
using Assignment1_Service;
using Assignment1_Service.Models;
using Assignment1_Service.Infrastructure;
using Assignment1_Repository.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<EnforcePasswordChangeAttribute>();
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
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
app.MapHub<AppHub>("/hubs/app");

app.Run();
