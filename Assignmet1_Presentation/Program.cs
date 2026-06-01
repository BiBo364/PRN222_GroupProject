using Assignmet1_Presentation.Models;

using Assignment1_Service;
using Assignment1_Service.Models;

using Assignment1_Service.Services.Interfaces;

using Microsoft.Extensions.Options;



var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllersWithViews();

builder.Services.AddAssignment1Services(builder.Configuration);

builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection("Gemini"));



builder.Services.Configure<PaymentSettings>(

    builder.Configuration.GetSection(PaymentSettings.SectionName));



builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>

{

    options.IdleTimeout = TimeSpan.FromMinutes(30);

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;

});



var app = builder.Build();



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



app.Run();


