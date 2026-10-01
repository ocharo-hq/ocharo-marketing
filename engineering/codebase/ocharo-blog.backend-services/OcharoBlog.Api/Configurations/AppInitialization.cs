using OcharoBlog.Infrastructure.Blog;
using OcharoBlog.Persistence;

namespace OcharoBlog.Api.Configurations;

/// <summary>Startup initialization — creates the schema and seeds an empty blog from the standalone journal.</summary>
public static class AppInitialization
{
    public static async Task InitializeAsync(this WebApplication app)
    {
        await app.Services.InitializeDatabaseAsync();
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<BlogSeeder>().SeedIfEmptyAsync(app.Environment.ContentRootPath, CancellationToken.None);
    }
}
