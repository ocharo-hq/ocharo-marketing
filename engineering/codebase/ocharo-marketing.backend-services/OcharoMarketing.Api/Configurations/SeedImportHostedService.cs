using System.Text.Json;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Marketing.Services;
using OcharoMarketing.Persistence;

namespace OcharoMarketing.Api.Configurations;

/// <summary>Applies durable schema migrations and imports the reviewed local source bundle once. With WoW2 Files the seed
/// assets upload as Marketing itself; an unreachable Files leaves the missing ones for the next boot, never the boot.</summary>
public sealed class SeedImportHostedService(IServiceProvider services, IWebHostEnvironment environment, ILogger<SeedImportHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMarketingRepository>();
        var assets = scope.ServiceProvider.GetRequiredService<IAssetService>();
        var seedRoot = Path.Combine(environment.ContentRootPath, "SeedData");
        var path = Path.Combine(seedRoot, "workspace-seed.json");
        if (!File.Exists(path)) path = Path.Combine(AppContext.BaseDirectory, "SeedData", "workspace-seed.json");
        seedRoot = Path.GetDirectoryName(path)!;
        using var source = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var brand = source.RootElement.GetProperty("brand").Deserialize<BrandModel>(MarketingJson.Options)!;
        var products = source.RootElement.GetProperty("products").Deserialize<ProductModel[]>(MarketingJson.Options)!;
        foreach (var entry in source.RootElement.GetProperty("assets").EnumerateArray())
        {
            var asset = entry.Deserialize<AssetModel>(MarketingJson.Options)!;
            if (await repository.FindAssetAsync(asset.Id, CancellationToken.None) is not null) continue;
            var relativePath = entry.GetProperty("relativePath").GetString()!;
            var assetPath = Path.GetFullPath(Path.Combine(seedRoot, relativePath));
            if (!assetPath.StartsWith(Path.GetFullPath(seedRoot) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException("Seed assets must remain within their import bundle.");
            try
            {
                await assets.SaveAsync(await File.ReadAllBytesAsync(assetPath), asset.FileName, asset.Provenance, asset.Role, CancellationToken.None, asset.Id);
            }
            catch (MarketingException exception) when (exception.StatusCode >= 500)
            {
                logger.LogWarning("Marketing's seed assets wait for the next boot: {Reason}", exception.Message);
                break;
            }
        }
        await repository.SeedAsync(brand, products, CancellationToken.None);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
