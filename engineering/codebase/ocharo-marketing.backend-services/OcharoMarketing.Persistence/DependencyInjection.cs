using Microsoft.EntityFrameworkCore;
using OcharoMarketing.Application.Scanning;
using OcharoMarketing.Persistence.Scanning;
using WoW.Two.Sdk.Backend.Beta.Data.Migrations.Ef;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Persistence.Marketing.Repositories;

namespace OcharoMarketing.Persistence;

/// <summary>Registers the local SQLite persistence layer.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var dataRoot = Path.GetFullPath(configuration["Marketing:DataDirectory"] ?? "App_Data");
        Directory.CreateDirectory(dataRoot);
        var connectionString = configuration["ConnectionStrings:OcharoMarketing"]
            ?? $"Data Source={Path.Combine(dataRoot, "marketing.db")};Default Timeout=30";
        services.AddDbContext<OcharoMarketingDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IMarketingRepository, MarketingRepository>();
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        services.AddScoped<ISourceAnalysisRepository, SourceAnalysisRepository>();
        services.AddEfMigrationsRunner<OcharoMarketingDbContext>(options => options.MaxConnectAttempts = 1);
        return services;
    }

}
