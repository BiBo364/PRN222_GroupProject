using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Services;
using Assignment1_Service.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Assignment1_Service;

public static class DependencyInjection
{
    public static IServiceCollection AddAssignment1Services(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<RagEduContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserReposity, UserRepository>();
        services.AddScoped<IUserServices, UserServices>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        return services;
    }
}
