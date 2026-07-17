using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Services;
using Assignment1_Service.Services.Interfaces;
using Microsoft.Data.SqlClient;
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
        var connectionString = BuildConnectionString(configuration);
        services.AddDbContext<RagEduContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.CommandTimeout(180)));

        services.AddScoped<IUserReposity, UserRepository>();
        services.AddScoped<IUserServices, UserServices>();
        services.AddScoped<IAccountNotificationService, AccountNotificationService>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<ILearningRepository, LearningRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddHttpClient<IEmbeddingService, EmbeddingService>();
        services.AddHttpClient<IGeminiClient, GeminiClient>();
        services.AddScoped<IGeminiService, GeminiService>();
        services.AddScoped<IStudyContentAiService, GeminiStudyContentAiService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ILearningService, LearningService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddHttpClient<IMomoPaymentService, MomoPaymentService>();

        return services;
    }

    private static string BuildConnectionString(IConfiguration configuration)
    {
        var configuredConnectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:DefaultConnection.");

        var builder = new SqlConnectionStringBuilder(configuredConnectionString);
        if (builder.DataSource.Contains(@"\SQLEXPRESS", StringComparison.OrdinalIgnoreCase)
            || builder.DataSource.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || builder.DataSource.StartsWith("(localdb)", StringComparison.OrdinalIgnoreCase))
        {
            // Một số bản SQL Server Express cũ trên máy học tập không hỗ trợ TLS mà
            // Microsoft.Data.SqlClient mới yêu cầu khi Encrypt=True.
            builder["Encrypt"] = false;
            builder.TrustServerCertificate = true;
        }

        return builder.ConnectionString;
    }
}
