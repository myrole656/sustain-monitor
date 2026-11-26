using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SustainMonitor.Infrastructure.Data;
using SustainMonitor.Infrastructure.Email;
using SustainMonitor.Infrastructure.Storage;

namespace SustainMonitor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, string contentRoot)
    {
        var connectionString = config.GetConnectionString("Default") ?? "Data Source=sustainmonitor.db";
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));

        var evidenceFolder = config["Storage:EvidenceFolder"];
        if (string.IsNullOrWhiteSpace(evidenceFolder))
            evidenceFolder = Path.Combine(contentRoot, "App_Data", "evidence");
        services.AddSingleton<IFileStore>(new LocalFileStore(evidenceFolder));

        services.Configure<SmtpOptions>(config.GetSection("Smtp"));
        services.AddSingleton<EmailSender>();
        services.AddSingleton<IAppEmailSender>(sp => sp.GetRequiredService<EmailSender>());
        services.AddSingleton<IEmailSender<AppUser>>(sp => sp.GetRequiredService<EmailSender>());
        return services;
    }
}
