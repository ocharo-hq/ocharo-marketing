using System.Text.Json;
using OcharoMarketing.Application.Assets.Models;
using OcharoMarketing.Application.Assets.Services;
using OcharoMarketing.Application.Core;
using OcharoMarketing.Application.Core.Errors;
using OcharoMarketing.Application.Core.Models;
using OcharoMarketing.Application.Core.Repositories;
using OcharoMarketing.Application.Products.Models;
using OcharoMarketing.Infrastructure.Import;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;

namespace OcharoMarketing.Api.Configurations;

/// <summary>Imports the reviewed local source bundle once. With WoW2 Files the seed assets upload as Marketing itself;
/// an unreachable Files leaves the missing ones for the next boot, never the boot. While an earlier release's SQLite
/// store awaits import-sqlite, it seeds nothing, so the import finds the schema empty.</summary>
public sealed class SeedImportHostedService(
    IServiceProvider services, IWebHostEnvironment environment, SqliteImportGate importGate,
    ILogger<SeedImportHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (importGate.IsPending)
        {
            logger.LogInformation("Marketing's seed waits for import-sqlite to import {Store}.", importGate.SourcePath);
            return;
        }
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
            if (
                !assetPath.StartsWith(
                    Path.GetFullPath(seedRoot) + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("Seed assets must remain within their import bundle.");
            try
            {
                await assets.SaveAsync(
                    await File.ReadAllBytesAsync(assetPath),
                    asset.FileName,
                    asset.Provenance,
                    asset.Role,
                    CancellationToken.None,
                    asset.Id);
            }
            catch (AppException exception) when (MarketingErrors.IsUpstreamFailure(exception.Error))
            {
                logger.LogWarning("Marketing's seed assets wait for the next boot: {Reason}", exception.Message);
                break;
            }
        }
        await repository.SeedAsync(brand, products, CancellationToken.None);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
