using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OcharoBlog.Application.Abstractions;
using SkiaSharp;

namespace OcharoBlog.Tests;

/// <summary>In-memory host over a temp SQLite file and blob folder; the real journal seed; a controllable clock.</summary>
public sealed class BlogHarness : IAsyncDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "ocharo-blog-tests", Guid.NewGuid().ToString("N"));
    public FixedClock Clock { get; } = new();
    public WebApplicationFactory<Program> Factory { get; }

    public BlogHarness(bool seed = true, bool seedImages = true, string? catalogueUrl = null, string? brandUrl = null,
        Action<IServiceCollection>? services = null, bool relativeSeedAssets = false, string? catalogueAdminUrl = null)
    {
        var assets = Path.Combine(Root, "assets");
        Directory.CreateDirectory(assets);
        if (seedImages)
        {
            byte shade = 10;
            foreach (var name in new[] { "och-logo.png", "och-collection.png", "och-collection-study.png", "och-nimcha.png", "och-cape.png" })
                File.WriteAllBytes(Path.Combine(assets, name), Png(shade += 30));
        }

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OcharoBlog"] = $"Data Source={Path.Combine(Root, "blog.db")};Pooling=False",
                ["Blog:DataRoot"] = Path.Combine(Root, "data"),
                ["Blog:SeedFile"] = seed ? "SeedData/journal-seed.json" : Path.Combine(Root, "missing.json"),
                ["Blog:SeedAssetRoot"] = relativeSeedAssets ? Path.GetRelativePath(context.HostingEnvironment.ContentRootPath, assets) : assets,
                ["Blog:CatalogueBaseUrl"] = catalogueUrl ?? "",
                ["Blog:BrandBaseUrl"] = brandUrl ?? "",
                ["Blog:RunScheduler"] = "false",
                ["Blog:CatalogueAdminUrl"] = catalogueAdminUrl,
            }));
            builder.ConfigureServices(collection =>
            {
                collection.Replace(ServiceDescriptor.Singleton<IClock>(Clock));
                services?.Invoke(collection);
            });
        });
    }

    public HttpClient Client() => Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public T Get<T>() where T : notnull => Factory.Services.CreateScope().ServiceProvider.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        try { Directory.Delete(Root, true); } catch (IOException) { }
    }

    public static byte[] Png(byte shade)
    {
        using var bitmap = new SKBitmap(40, 60, SKColorType.Rgba8888, SKAlphaType.Premul);
        bitmap.Erase(new SKColor(shade, (byte)(255 - shade), 120, 255));
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }
}

/// <summary>Deterministic, movable clock.</summary>
public sealed class FixedClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 9, 28, 9, 0, 0, TimeSpan.Zero);
}
