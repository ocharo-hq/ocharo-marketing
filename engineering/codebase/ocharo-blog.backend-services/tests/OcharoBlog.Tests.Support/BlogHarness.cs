using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using SkiaSharp;

namespace OcharoBlog.Tests.Support;

/// <summary>In-memory host over its own PostgreSQL database (<see cref="BlogDatabase"/>) and a temp blob folder; the
/// real journal seed; a controllable clock; test sign-in.</summary>
/// <remarks>The host boots on first use, so a test may first put an earlier release's SQLite store at
/// <see cref="StorePath"/>.</remarks>
public sealed class BlogHarness : IAsyncDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "ocharo-blog-tests", Guid.NewGuid().ToString("N"));
    /// <summary>The instant every harness's <see cref="Clock"/> starts at.</summary>
    public static readonly DateTimeOffset Start = new(2026, 9, 28, 9, 0, 0, TimeSpan.Zero);

    public FakeTimeProvider Clock { get; } = new(Start);
    public WebApplicationFactory<Program> Factory { get; }

    /// <summary>The host's PostgreSQL database.</summary>
    public string Database { get; } = BlogDatabase.Create();

    /// <summary>Where the host looks for the SQLite store an earlier release kept
    /// (<c>ConnectionStrings:OcharoBlog</c>); absent unless a test puts one there.</summary>
    public string StorePath => Path.Combine(Root, "blog.db");

    public BlogHarness(bool seed = true, bool seedImages = true, string? catalogueUrl = null, string? brandUrl = null,
        Action<IServiceCollection>? services = null, bool relativeSeedAssets = false, string? catalogueAdminUrl = null)
    {
        var assets = Path.Combine(Root, "assets");
        Directory.CreateDirectory(assets);
        if (seedImages)
        {
            byte shade = 10;
            foreach (var name in new[]
                {
                    "och-logo.png",
                    "och-collection.png",
                    "och-collection-study.png",
                    "och-nimcha.png",
                    "och-cape.png"
                })
                File.WriteAllBytes(Path.Combine(assets, name), Png(shade += 30));
        }

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            EditorialSignIn.UseBootSettings(builder);
            builder.ConfigureAppConfiguration((context, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>(EditorialSignIn.Settings)
            {
                ["ConnectionStrings:OcharoBlog"] = $"Data Source={StorePath};Pooling=False",
                ["Blog:DataRoot"] = Path.Combine(Root, "data"),
                ["Blog:SeedFile"] = seed ? "SeedData/journal-seed.json" : Path.Combine(Root, "missing.json"),
                ["Blog:SeedAssetRoot"] =
                    relativeSeedAssets
                        ? Path.GetRelativePath(context.HostingEnvironment.ContentRootPath, assets)
                        : assets,
                ["Blog:CatalogueBaseUrl"] = catalogueUrl ?? "",
                ["Blog:BrandBaseUrl"] = brandUrl ?? "",
                ["Blog:SchedulerSeconds"] = "0",
                ["Blog:CatalogueAdminUrl"] = catalogueAdminUrl,
            }).AddInMemoryCollection(BlogDatabase.Settings(Database)));
            builder.ConfigureServices(collection =>
            {
                EditorialSignIn.Use(collection);
                collection.Replace(ServiceDescriptor.Singleton<TimeProvider>(Clock));
                services?.Invoke(collection);
            });
        });
    }

    /// <summary>A client signed in as the owner, who may do anything; the access rules have their own tests.</summary>
    public HttpClient Client() => EditorialSignIn.Client(Factory, EditorialSignIn.Owner);

    /// <summary>A client without a session.</summary>
    public HttpClient Anonymous() => EditorialSignIn.Anonymous(Factory);

    public T Get<T>() where T : notnull => Factory.Services.CreateScope().ServiceProvider.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await BlogDatabase.DropAsync(Database);
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
