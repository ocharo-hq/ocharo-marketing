using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using OcharoBlog.Infrastructure.Blog;
using SkiaSharp;
using WoW.Two.Sdk.Beta.Be.Files.Client;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Http.Auth.OAuth2TokenExchange;

namespace OcharoBlog.Tests;

/// <summary>Editorial on WoW2 Files (<c>Files:BaseUrl</c> set): media land in Files once, the media routes keep their own
/// public-or-private decision and redirect to a short-lived Files link, edits and static exports read the original from
/// Files, and an image stored before Files reads its local bytes until the backfill fills its id. Files itself is
/// <see cref="TestFiles"/>, an in-memory broker set. Edits take the editor's process-wide capacity, hence the collection.</summary>
[Collection(MediaEditingCapacity.Name)]
public sealed class FilesMediaTests : IAsyncDisposable
{
    private static readonly MediaEditRecipe Recipe = new(new(0, 0, 20, 30), 90, false, false, null, new("png", 90), false);

    private readonly string root = Path.Combine(Path.GetTempPath(), "ocharo-blog-files-tests", Guid.NewGuid().ToString("N"));
    private readonly List<IAsyncDisposable> hosts = [];

    public async ValueTask DisposeAsync()
    {
        foreach (var host in hosts)
            await host.DisposeAsync();
        try { Directory.Delete(root, true); } catch (IOException) { }
    }

    [Fact]
    public async Task An_image_lands_in_files_once_and_opens_as_a_link_with_its_preview_as_a_variant()
    {
        var files = new TestFiles();
        var host = Host(files);
        var bytes = BlogHarness.Png(70);
        var sha = Sha(bytes);

        var stored = await Get<IMediaStore>(host).StoreAsync(bytes, "cover.png", default);
        var again = await Get<IMediaStore>(host).StoreAsync(bytes, "cover-again.png", default);
        var file = files.Model(stored.FileId!);
        var original = await Get<IMediaStore>(host).OpenAsync(sha, false, default);
        var preview = await Get<IMediaStore>(host).OpenAsync(sha, true, default);

        Assert.IsType<FilesMediaStore>(Get<IMediaStore>(host));
        Assert.Equal(1, files.Uploads);
        Assert.Equal(stored.FileId, again.FileId);
        Assert.Equal("editorial", file.AppId);
        Assert.Equal(sha, file.Sha256);
        Assert.Contains(new FileReference("editorial.media", sha), files.ReferencesOf(file.Id));
        Assert.Equal("cover.png", stored.FileName);
        Assert.Equal(40, stored.Width);
        Assert.Equal(60, stored.Height);
        Assert.True(MediaPreview.Exists(stored));
        Assert.Null(stored.PreviewPath);
        Assert.Equal(TestFiles.Link(file.Id, null), original?.Location);
        Assert.Equal("image/png", original?.ContentType);
        Assert.Equal(TestFiles.Link(file.Id, 1400), preview?.Location);
        Assert.Equal("image/webp", preview?.ContentType);
        Assert.False(Directory.Exists(Path.Combine(root, "data", "blobs", "media")));
        Assert.False(Directory.Exists(Path.Combine(root, "data", "blobs", "previews")));
    }

    [Fact]
    public async Task The_media_route_keeps_deciding_privacy_and_redirects_without_caching()
    {
        var files = new TestFiles();
        var host = Host(files, seed: true);
        using var anonymous = EditorialSignIn.Anonymous(host);
        using var owner = EditorialSignIn.Client(host, EditorialSignIn.Owner);
        using var outsider = EditorialSignIn.Client(host, EditorialSignIn.Outsider);

        var published = Regex.Match(await anonymous.GetStringAsync("/"), "/media/([0-9a-f]{64})/preview");
        using var cover = await anonymous.GetAsync(published.Value);
        var draft = (await UploadAsync(owner, BlogHarness.Png(200))).GetProperty("sha256").GetString()!;
        using var hidden = await anonymous.GetAsync($"/media/{draft}");
        using var foreign = await outsider.GetAsync($"/media/{draft}");
        using var member = await owner.GetAsync($"/media/{draft}");
        var library = await owner.GetFromJsonAsync<JsonElement[]>("/api/blog/media");

        Assert.True(published.Success);
        Assert.Equal(HttpStatusCode.Redirect, cover.StatusCode);
        Assert.Equal(TestFiles.Link(await FileOfAsync(host, published.Groups[1].Value), 1400), cover.Headers.Location);
        Assert.True(cover.Headers.CacheControl?.NoStore);
        Assert.Equal(HttpStatusCode.Unauthorized, hidden.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, member.StatusCode);
        Assert.Equal(TestFiles.Link(await FileOfAsync(host, draft), null), member.Headers.Location);
        Assert.True(member.Headers.CacheControl?.NoStore);
        Assert.All(library!, media => Assert.Equal($"/media/{media.GetProperty("sha256").GetString()}/preview", media.GetProperty("previewUrl").GetString()));
    }

    [Fact]
    public async Task An_edit_reads_its_original_from_files_and_stores_the_derivative_there_with_its_receipt()
    {
        var files = new TestFiles();
        var host = Host(files);
        using var owner = EditorialSignIn.Client(host, EditorialSignIn.Owner);
        var original = (await UploadAsync(owner, BlogHarness.Png(90))).GetProperty("sha256").GetString()!;

        using var preview = await owner.PostAsJsonAsync($"/api/blog/media/{original}/edits/preview", Recipe);
        using var saved = await owner.PostAsJsonAsync($"/api/blog/media/{original}/edits", Recipe);
        saved.EnsureSuccessStatusCode();
        var edited = (await saved.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sha256").GetString()!;
        var history = await owner.GetFromJsonAsync<JsonElement[]>($"/api/blog/media/{edited}/edits");
        var record = await Get<IBlogRepository>(host).FindMediaAsync(edited, default);
        var derivative = files.Model(record!.FileId!);

        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.Equal(2, files.Reads);
        Assert.Equal(2, files.Uploads);
        Assert.Equal(edited, derivative.Sha256);
        Assert.Equal("editorial", derivative.AppId);
        Assert.Contains(new FileReference("editorial.media", edited), files.ReferencesOf(derivative.Id));
        Assert.Equal(original, Assert.Single(history!).GetProperty("sourceSha256").GetString());
        Assert.Equal(30, record.Width);
        Assert.Equal(20, record.Height);
    }

    [Fact]
    public async Task A_static_export_renders_each_preview_from_the_original_in_files()
    {
        var files = new TestFiles();
        var host = Host(files, seed: true);
        using var owner = EditorialSignIn.Client(host, EditorialSignIn.Owner);

        using var export = new ZipArchive(new MemoryStream(await owner.GetByteArrayAsync("/api/blog/export/site.zip")));
        var media = export.Entries.Where(entry => entry.FullName.StartsWith("media/", StringComparison.Ordinal)).ToList();

        Assert.True(media.Count >= 3);
        Assert.Equal(media.Count, files.Reads);
        foreach (var entry in media)
        {
            using var buffer = new MemoryStream();
            await using (var stream = entry.Open())
                await stream.CopyToAsync(buffer);
            using var codec = SKCodec.Create(new MemoryStream(buffer.ToArray()));
            Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
        }
    }

    [Fact]
    public async Task An_image_stored_before_files_reads_locally_until_the_backfill_fills_its_id()
    {
        var files = new TestFiles();
        var host = Host(files);
        using var owner = EditorialSignIn.Client(host, EditorialSignIn.Owner);
        var bytes = BlogHarness.Png(80);
        var sha = Sha(bytes);
        await Get<MediaStore>(host).StoreAsync(bytes, "legacy.png", default);

        var local = await Get<IMediaStore>(host).OpenAsync(sha, false, default);
        using var served = await owner.GetAsync($"/media/{sha}");
        await Get<FilesMediaBackfill>(host).RunAsync(default);
        var unmoved = await Get<IBlogRepository>(host).FindMediaAsync(sha, default);
        var moved = files.Add(bytes, "editorial", new FileReference("editorial.media", sha));
        await Get<FilesMediaBackfill>(host).RunAsync(default);
        var filled = await Get<IBlogRepository>(host).FindMediaAsync(sha, default);
        var linked = await Get<IMediaStore>(host).OpenAsync(sha, true, default);

        Assert.NotNull(local?.Content);
        await using (local!.Content)
            Assert.Equal(sha, Convert.ToHexStringLower(await SHA256.HashDataAsync(local.Content!)));
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal(bytes, await served.Content.ReadAsByteArrayAsync());
        Assert.Null(unmoved?.FileId);
        Assert.Equal(0, files.Uploads);
        Assert.Equal(moved.Id.ToString(), filled?.FileId);
        Assert.Equal(TestFiles.Link(moved.Id, 1400), linked?.Location);
    }

    [Fact]
    public async Task The_boot_fills_moved_ids_and_keeps_running_when_files_is_unreachable()
    {
        var moving = BlogHarness.Png(110);
        var staying = BlogHarness.Png(120);
        var local = Host(null);
        await Get<IMediaStore>(local).StoreAsync(moving, "moving.png", default);
        await Get<IMediaStore>(local).StoreAsync(staying, "staying.png", default);
        await local.DisposeAsync();
        hosts.Remove(local);

        var down = new TestFiles { Unreachable = true };
        var unreachable = Host(down);
        using var member = EditorialSignIn.Client(unreachable, EditorialSignIn.Owner);
        using var health = await member.GetAsync("/health");
        using var served = await member.GetAsync($"/media/{Sha(staying)}");
        await unreachable.DisposeAsync();
        hosts.Remove(unreachable);

        var migrated = new TestFiles();
        var moved = migrated.Add(moving, "editorial", new FileReference("editorial.media", Sha(moving)));
        var booted = Host(migrated);
        var filled = await Get<IBlogRepository>(booted).FindMediaAsync(Sha(moving), default);
        var waiting = await Get<IBlogRepository>(booted).FindMediaAsync(Sha(staying), default);

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal(staying, await served.Content.ReadAsByteArrayAsync());
        Assert.Equal(moved.Id.ToString(), filled?.FileId);
        Assert.Null(waiting?.FileId);
    }

    [Fact]
    public async Task A_public_render_stores_the_brand_logo_through_files_once()
    {
        var logo = BlogHarness.Png(210);
        var sha = Sha(logo);
        var files = new TestFiles();
        var brand = new LogoKit(new BrandSnapshot(7, sha, "https://brand.example.test/api/public/brand/media/" + sha, []), logo);
        var host = Host(files, services: services => services.Replace(ServiceDescriptor.Singleton<IBrandKit>(brand)));
        using var anonymous = EditorialSignIn.Anonymous(host);

        var first = await anonymous.GetStringAsync("/");
        var second = await anonymous.GetStringAsync("/journal/");
        var record = await Get<IBlogRepository>(host).FindMediaAsync(sha, default);
        using var preview = await anonymous.GetAsync($"/media/{sha}/preview");

        Assert.Contains($"/media/{sha}/preview", first);
        Assert.Contains($"/media/{sha}/preview", second);
        Assert.Equal(1, brand.Downloads);
        Assert.Equal(1, files.Uploads);
        Assert.Contains(new FileReference("editorial.media", sha), files.ReferencesOf(await FileOfAsync(host, sha)));
        Assert.Equal("ocharo-logo-r7.png", record?.FileName);
        Assert.Equal(HttpStatusCode.Redirect, preview.StatusCode);
        Assert.Equal(TestFiles.Link(await FileOfAsync(host, sha), 1400), preview.Headers.Location);
    }

    [Fact]
    public async Task A_public_render_references_brands_logo_file_without_copying_it_and_the_logo_opens_anonymously()
    {
        var logo = BlogHarness.Png(230);
        var sha = Sha(logo);
        var files = new TestFiles();
        var brandFile = files.Add(logo, "brand", new FileReference("brand.media", sha));
        var brand = new LogoKit(new BrandSnapshot(8, sha, "https://brand.example.test/api/public/brand/media/" + sha, [], LogoFileId: brandFile.Id.ToString()), logo);
        var host = Host(files, services: services => services.Replace(ServiceDescriptor.Singleton<IBrandKit>(brand)));
        using var anonymous = EditorialSignIn.Anonymous(host);

        var first = await anonymous.GetStringAsync("/");
        var second = await anonymous.GetStringAsync("/journal/");
        using var preview = await anonymous.GetAsync($"/media/{sha}/preview");
        using var original = await anonymous.GetAsync($"/media/{sha}");
        var record = await Get<IBlogRepository>(host).FindMediaAsync(sha, default);

        Assert.Contains($"/media/{sha}/preview", first);
        Assert.Contains($"/media/{sha}/preview", second);
        Assert.Equal(0, brand.Downloads);
        Assert.Equal(0, files.Uploads);
        Assert.Empty(files.Of("editorial"));
        Assert.Equal(brandFile.Id.ToString(), record?.FileId);
        Assert.Equal("ocharo-logo-r8.png", record?.FileName);
        Assert.Equal("image/png", record?.ContentType);
        Assert.Equal(logo.LongLength, record?.ByteLength);
        Assert.Equal(2, files.ReferencesOf(brandFile.Id).Count);
        Assert.Contains(new FileReference("editorial.media", sha), files.ReferencesOf(brandFile.Id));
        Assert.Equal(HttpStatusCode.Redirect, preview.StatusCode);
        Assert.Equal(TestFiles.Link(brandFile.Id, 1400), preview.Headers.Location);
        Assert.Equal(HttpStatusCode.Redirect, original.StatusCode);
        Assert.Equal(TestFiles.Link(brandFile.Id, null), original.Headers.Location);
    }

    [Fact]
    public async Task A_logo_row_that_already_has_a_file_is_left_alone()
    {
        var logo = BlogHarness.Png(240);
        var sha = Sha(logo);
        var files = new TestFiles();
        var brandFile = files.Add(logo, "brand", new FileReference("brand.media", sha));
        var brand = new LogoKit(new BrandSnapshot(9, sha, "https://brand.example.test/api/public/brand/media/" + sha, [], LogoFileId: brandFile.Id.ToString()), logo);
        var host = Host(files, services: services => services.Replace(ServiceDescriptor.Singleton<IBrandKit>(brand)));
        var copied = await Get<IMediaStore>(host).StoreAsync(logo, "ocharo-logo-r8.png", default);
        using var anonymous = EditorialSignIn.Anonymous(host);

        var home = await anonymous.GetStringAsync("/");
        var record = await Get<IBlogRepository>(host).FindMediaAsync(sha, default);

        Assert.Contains($"/media/{sha}/preview", home);
        Assert.Equal(copied.FileId, record?.FileId);
        Assert.NotEqual(brandFile.Id.ToString(), record?.FileId);
        Assert.Equal(1, files.Uploads);
        Assert.Equal(0, brand.Downloads);
        Assert.DoesNotContain(new FileReference("editorial.media", sha), files.ReferencesOf(brandFile.Id));
    }

    [Fact]
    public async Task A_brand_file_gone_from_files_falls_back_to_a_copy()
    {
        var logo = BlogHarness.Png(250);
        var sha = Sha(logo);
        var files = new TestFiles();
        var brand = new LogoKit(new BrandSnapshot(10, sha, "https://brand.example.test/api/public/brand/media/" + sha, [], LogoFileId: FileId.New().ToString()), logo);
        var host = Host(files, services: services => services.Replace(ServiceDescriptor.Singleton<IBrandKit>(brand)));
        using var anonymous = EditorialSignIn.Anonymous(host);

        var home = await anonymous.GetStringAsync("/");
        var copy = Assert.Single(files.Of("editorial"));

        Assert.Contains($"/media/{sha}/preview", home);
        Assert.Equal(1, brand.Downloads);
        Assert.Equal(sha, copy.Sha256);
        Assert.Equal(copy.Id, await FileOfAsync(host, sha));
    }

    [Fact]
    public async Task The_files_client_signs_in_as_editorial_and_exchanges_the_session_token()
    {
        var host = Host(new TestFiles(), settings: new Dictionary<string, string?> { ["Identity:Client:ClientSecret"] = "editorial-test-secret" });

        var options = Get<IOptions<FilesClientOptions>>(host).Value;
        var subject = host.Services.GetRequiredService<IOAuth2SubjectTokenSource>();

        Assert.Equal(new Uri(TestFiles.Url), options.BaseAddress);
        Assert.Equal(new Uri("https://identity.example.test/connect/token"), options.Identity?.TokenEndpoint);
        Assert.Equal("editorial", options.Identity?.ClientId);
        // The BFF's own source (internal to the SDK) reads the session's token; the default would read a bearer header.
        Assert.Equal("BffSubjectTokenSource", subject.GetType().Name);
    }

    /// <summary>An Editorial host over this test's SQLite file and data folder; <paramref name="files"/> set turns WoW2 Files
    /// on and stands in for it.</summary>
    private WebApplicationFactory<Program> Host(TestFiles? files, bool seed = false, Action<IServiceCollection>? services = null,
        IReadOnlyDictionary<string, string?>? settings = null)
    {
        var assets = Path.Combine(root, "assets");
        Directory.CreateDirectory(assets);
        byte shade = 10;
        foreach (var name in new[] { "och-logo.png", "och-collection.png", "och-collection-study.png", "och-nimcha.png", "och-cape.png" })
            File.WriteAllBytes(Path.Combine(assets, name), BlogHarness.Png(shade += 30));

        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            EditorialSignIn.UseBootSettings(builder);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>(EditorialSignIn.Settings)
            {
                ["ConnectionStrings:OcharoBlog"] = $"Data Source={Path.Combine(root, "blog.db")};Pooling=False",
                ["Blog:DataRoot"] = Path.Combine(root, "data"),
                ["Blog:SeedFile"] = seed ? "SeedData/journal-seed.json" : Path.Combine(root, "missing.json"),
                ["Blog:SeedAssetRoot"] = assets,
                ["Blog:CatalogueBaseUrl"] = "",
                ["Blog:BrandBaseUrl"] = "",
                ["Blog:SchedulerSeconds"] = "0",
                ["Files:BaseUrl"] = files is null ? null : TestFiles.Url,
            }).AddInMemoryCollection(settings ?? new Dictionary<string, string?>()));
            builder.ConfigureServices(collection => EditorialSignIn.Use(collection));
            builder.ConfigureTestServices(collection =>
            {
                files?.Register(collection);
                services?.Invoke(collection);
            });
        });
        hosts.Add(factory);
        return factory;
    }

    private static T Get<T>(WebApplicationFactory<Program> host) where T : notnull =>
        host.Services.CreateScope().ServiceProvider.GetRequiredService<T>();

    private static async Task<FileId> FileOfAsync(WebApplicationFactory<Program> host, string sha) =>
        FileId.TryParse((await Get<IBlogRepository>(host).FindMediaAsync(sha, default))?.FileId ?? "", out var id) ? id : default;

    private static async Task<JsonElement> UploadAsync(HttpClient client, byte[] bytes)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "upload.png");
        using var response = await client.PostAsync("/api/blog/media", form);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>A published brand kit whose logo download is counted.</summary>
    private sealed class LogoKit(BrandSnapshot snapshot, byte[] logo) : IBrandKit
    {
        public int Downloads { get; private set; }

        public Task<BrandSnapshot?> GetAsync(CancellationToken ct) => Task.FromResult<BrandSnapshot?>(snapshot);

        public Task<byte[]?> DownloadAsync(string url, CancellationToken ct)
        {
            Downloads++;
            return Task.FromResult<byte[]?>(logo);
        }
    }
}
