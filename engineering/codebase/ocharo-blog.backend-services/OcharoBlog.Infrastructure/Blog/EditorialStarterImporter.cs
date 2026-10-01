using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Explicit, idempotent import of source-documented research into drafts, never publication.</summary>
public sealed class EditorialStarterImporter(IBlogRepository repository, ArticleService articles, IMediaStore media, ILogger<EditorialStarterImporter> logger)
{
    public async Task<StarterImportResult> ImportAsync(string contentRoot,CancellationToken ct)
    {
        var seedPath=Path.Combine(contentRoot,"SeedData","editorial-starter.json");
        if(!File.Exists(seedPath))throw new BlogException("Editorial research pack is not installed.");
        using var seed=JsonDocument.Parse(await File.ReadAllTextAsync(seedPath,ct));
        if(seed.RootElement.GetProperty("schemaVersion").GetInt32()!=1)throw new BlogException("Unsupported research pack.");
        var created=new List<Guid>();var skipped=new List<string>();
        foreach(var element in seed.RootElement.GetProperty("articles").EnumerateArray())
        {
            var input=element.Deserialize<ArticleInput>(new JsonSerializerOptions(JsonSerializerDefaults.Web))??throw new BlogException("Invalid research item.");
            if(await repository.FindArticleBySlugAsync(input.Slug,ct)is not null){skipped.Add(input.Slug);continue;}
            if(element.TryGetProperty("asset",out var asset))
            {
                var name=Path.GetFileName(asset.GetProperty("fileName").GetString()!);
                var path=Path.Combine(contentRoot,"SeedData","EditorialAssets",name);
                var bytes=await File.ReadAllBytesAsync(path,ct);var sha=Convert.ToHexStringLower(SHA256.HashData(bytes));
                if(sha!=asset.GetProperty("sha256").GetString())throw new BlogException("Research image checksum differs from its source register.");
                var stored=await media.StoreAsync(bytes,name,ct);
                input=input with {Cover=new ArticleCover(stored.Sha256,element.GetProperty("coverAlt").GetString()!,element.GetProperty("coverCaption").GetString()!,null),
                    ImageProvenance=asset.GetProperty("provenance").Deserialize<ImageProvenance>(new JsonSerializerOptions(JsonSerializerDefaults.Web))};
            }
            var item=await articles.CreateAsync(input with {ChangeSummary="Imported source-documented research as a draft"},ct);created.Add(item.Id);
        }
        logger.LogInformation("Imported {Count} editorial drafts; preserved {Skipped} existing records",created.Count,skipped.Count);
        return new(created,skipped);
    }
}
public sealed record StarterImportResult(IReadOnlyList<Guid> Created,IReadOnlyList<string> Skipped);
