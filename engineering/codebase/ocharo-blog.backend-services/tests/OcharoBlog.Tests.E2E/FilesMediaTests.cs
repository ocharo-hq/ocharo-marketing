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

namespace OcharoBlog.Tests.E2E;

/// <summary>Editorial on WoW2 Files (<c>Files:BaseUrl</c> set): media land in Files once, the media routes keep their
/// own
/// public-or-private decision and redirect to a short-lived Files link, edits and static exports read the original from
/// Files, and an image stored before Files reads its local bytes until the backfill fills its id. Files itself is
/// <see cref="TestFiles"/>, an in-memory broker set. Edits take the editor's process-wide capacity, hence the
/// collection.</summary>
[Collection(MediaEditingCapacity.Name)]
public sealed class FilesMediaTests : IAsyncDisposable
{
    private static readonly MediaEditRecipe Recipe =
        new(new(0, 0, 20, 30), 90, false, false, null, new("png", 90), false);

    private readonly string root =
        Path.Combine(Path.GetTempPath(), "ocharo-blog-files-tests", Guid.NewGuid().ToString("N"));
    private readonly string database = BlogDatabase.Create();
    private readonly List<IAsyncDisposable> hosts = [];

    public async ValueTask DisposeAsync()
    {
        foreach (var host in hosts)
            await host.DisposeAsync();
        await BlogDatabase.DropAsync(database);
        try { Directory.Delete(root, true); } catch (IOException) { }
    }

    [Fact]
    public async Task Upload_ShouldStoreOnceAndOpenAsALink_WhenFilesIsOn()
    {
        var files = new TestFiles();
        var host = Host(files);
        var bytes = BlogHarness.Png(70);
        var sha = Sha(bytes);

        var stored = (await Get<IMediaStore>(host).StoreAsync(bytes, "cover.png", default)).ValueOrThrow();
        var again = (await Get<IMediaStore>(host).StoreAsync(bytes, "cover-again.png", default)).ValueOrThrow();
        var file = files.Model(stored.FileId!);
        var original = await Get<IMediaStore>(host).OpenAsync(sha, false, default);
        var preview = await Get<IMediaStore>(host).OpenAsync(sha, true, default);

        Get<IMediaStore>(host).Should().BeOfType<FilesMediaStore>();
        files.Uploads.Should().Be(1);
        again.FileId.Should().Be(stored.FileId);
        file.AppId.Should().Be("editorial");
        file.Sha256.Should().Be(sha);
        files.ReferencesOf(file.Id).Should().Contain(new FileReference("editorial.media", sha));
        stored.FileName.Should().Be("cover.png");
        stored.Width.Should().Be(40);
        stored.Height.Should().Be(60);
        MediaPreview.Exists(stored).Should().BeTrue();
        stored.PreviewPath.Should().BeNull();
        (original?.Location).Should().Be(TestFiles.Link(file.Id, null));
        (original?.ContentType).Should().Be("image/png");
        (preview?.Location).Should().Be(TestFiles.Link(file.Id, 1400));
        (preview?.ContentType).Should().Be("image/webp");
        Directory.Exists(Path.Combine(root, "data", "blobs", "media")).Should().BeFalse();
        Directory.Exists(Path.Combine(root, "data", "blobs", "previews")).Should().BeFalse();
    }

    [Fact]
    public async Task MediaRoute_ShouldRedirectWithoutCaching_WhenTheFileIsInFiles()
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

        published.Success.Should().BeTrue();
        cover.StatusCode.Should().Be(HttpStatusCode.Redirect);
        cover.Headers.Location.Should().Be(TestFiles.Link(await FileOfAsync(host, published.Groups[1].Value), 1400));
        (cover.Headers.CacheControl?.NoStore).Should().BeTrue();
        hidden.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        foreign.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        member.StatusCode.Should().Be(HttpStatusCode.Redirect);
        member.Headers.Location.Should().Be(TestFiles.Link(await FileOfAsync(host, draft), null));
        (member.Headers.CacheControl?.NoStore).Should().BeTrue();
        library!.Should()
            .AllSatisfy(media =>
                media.GetProperty("previewUrl")
                    .GetString()
                    .Should()
                    .Be($"/media/{media.GetProperty("sha256").GetString()}/preview"));
    }

    [Fact]
    public async Task Edit_ShouldStoreItsDerivativeAndReceiptInFiles_WhenTheOriginalIsThere()
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

        preview.StatusCode.Should().Be(HttpStatusCode.OK);
        files.Reads.Should().Be(2);
        files.Uploads.Should().Be(2);
        derivative.Sha256.Should().Be(edited);
        derivative.AppId.Should().Be("editorial");
        files.ReferencesOf(derivative.Id).Should().Contain(new FileReference("editorial.media", edited));
        history!.Should().ContainSingle().Subject.GetProperty("sourceSha256").GetString().Should().Be(original);
        record.Width.Should().Be(30);
        record.Height.Should().Be(20);
    }

    [Fact]
    public async Task SiteExport_ShouldRenderEachPreviewFromTheOriginal_WhenFilesHoldsIt()
    {
        var files = new TestFiles();
        var host = Host(files, seed: true);
        using var owner = EditorialSignIn.Client(host, EditorialSignIn.Owner);

        using var export = new ZipArchive(new MemoryStream(await owner.GetByteArrayAsync("/api/blog/export/site.zip")));
        var media =
            export.Entries.Where(entry => entry.FullName.StartsWith("media/", StringComparison.Ordinal)).ToList();

        (media.Count >= 3).Should().BeTrue();
        files.Reads.Should().Be(media.Count);
        foreach (var entry in media)
        {
            using var buffer = new MemoryStream();
            await using (var stream = entry.Open())
                await stream.CopyToAsync(buffer);
            using var codec = SKCodec.Create(new MemoryStream(buffer.ToArray()));
            codec.EncodedFormat.Should().Be(SKEncodedImageFormat.Webp);
        }
    }

    [Fact]
    public async Task Media_ShouldReadLocally_WhenTheBackfillHasNotFilledItsFileId()
    {
        var files = new TestFiles();
        var host = Host(files);
        using var owner = EditorialSignIn.Client(host, EditorialSignIn.Owner);
        var bytes = BlogHarness.Png(80);
        var sha = Sha(bytes);
        (await Get<MediaStore>(host).StoreAsync(bytes, "legacy.png", default)).ValueOrThrow();

        var local = await Get<IMediaStore>(host).OpenAsync(sha, false, default);
        using var served = await owner.GetAsync($"/media/{sha}");
        await Get<FilesMediaBackfill>(host).RunAsync(default);
        var unmoved = await Get<IBlogRepository>(host).FindMediaAsync(sha, default);
        var moved = files.Add(bytes, "editorial", new FileReference("editorial.media", sha));
        await Get<FilesMediaBackfill>(host).RunAsync(default);
        var filled = await Get<IBlogRepository>(host).FindMediaAsync(sha, default);
        var linked = await Get<IMediaStore>(host).OpenAsync(sha, true, default);

        (local?.Content).Should().NotBeNull();
        await using (local!.Content)
            Convert.ToHexStringLower(await SHA256.HashDataAsync(local.Content!)).Should().Be(sha);
        served.StatusCode.Should().Be(HttpStatusCode.OK);
        (await served.Content.ReadAsByteArrayAsync()).Should().Equal(bytes);
        (unmoved?.FileId).Should().BeNull();
        files.Uploads.Should().Be(0);
        (filled?.FileId).Should().Be(moved.Id.ToString());
        (linked?.Location).Should().Be(TestFiles.Link(moved.Id, 1400));
    }

    [Fact]
    public async Task Boot_ShouldFillMovedFileIdsAndKeepRunning_WhenFilesIsUnreachable()
    {
        var moving = BlogHarness.Png(110);
        var staying = BlogHarness.Png(120);
        var local = Host(null);
        (await Get<IMediaStore>(local).StoreAsync(moving, "moving.png", default)).ValueOrThrow();
        (await Get<IMediaStore>(local).StoreAsync(staying, "staying.png", default)).ValueOrThrow();
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

        health.StatusCode.Should().Be(HttpStatusCode.OK);
        served.StatusCode.Should().Be(HttpStatusCode.OK);
        (await served.Content.ReadAsByteArrayAsync()).Should().Equal(staying);
        (filled?.FileId).Should().Be(moved.Id.ToString());
        (waiting?.FileId).Should().BeNull();
    }

    [Fact]
    public async Task PublicRender_ShouldStoreTheBrandLogoInFilesOnce_WhenBrandGivesNoFileId()
    {
        var logo = BlogHarness.Png(210);
        var sha = Sha(logo);
        var files = new TestFiles();
        var brand =
            new LogoKit(
                new BrandSnapshot(7, sha, "https://brand.example.test/api/public/brand/media/" + sha, []),
                logo);
        var host = Host(files, services: services => services.Replace(ServiceDescriptor.Singleton<IBrandKit>(brand)));
        using var anonymous = EditorialSignIn.Anonymous(host);

        var first = await anonymous.GetStringAsync("/");
        var second = await anonymous.GetStringAsync("/journal/");
        var record = await Get<IBlogRepository>(host).FindMediaAsync(sha, default);
        using var preview = await anonymous.GetAsync($"/media/{sha}/preview");

        first.Should().Contain($"/media/{sha}/preview");
        second.Should().Contain($"/media/{sha}/preview");
        brand.Downloads.Should().Be(1);
        files.Uploads.Should().Be(1);
        files.ReferencesOf(await FileOfAsync(host, sha)).Should().Contain(new FileReference("editorial.media", sha));
        (record?.FileName).Should().Be("ocharo-logo-r7.png");
        preview.StatusCode.Should().Be(HttpStatusCode.Redirect);
        preview.Headers.Location.Should().Be(TestFiles.Link(await FileOfAsync(host, sha), 1400));
    }

    [Fact]
    public async Task PublicRender_ShouldReferenceTheBrandLogoFile_WhenBrandKeepsItInFiles()
    {
        var logo = BlogHarness.Png(230);
        var sha = Sha(logo);
        var files = new TestFiles();
        var brandFile = files.Add(logo, "brand", new FileReference("brand.media", sha));
        var brand =
            new LogoKit(
                new BrandSnapshot(
                    8,
                    sha,
                    "https://brand.example.test/api/public/brand/media/" + sha,
                    [],
                    LogoFileId: brandFile.Id.ToString()),
                logo);
        var host = Host(files, services: services => services.Replace(ServiceDescriptor.Singleton<IBrandKit>(brand)));
        using var anonymous = EditorialSignIn.Anonymous(host);

        var first = await anonymous.GetStringAsync("/");
        var second = await anonymous.GetStringAsync("/journal/");
        using var preview = await anonymous.GetAsync($"/media/{sha}/preview");
        using var original = await anonymous.GetAsync($"/media/{sha}");
        var record = await Get<IBlogRepository>(host).FindMediaAsync(sha, default);

        first.Should().Contain($"/media/{sha}/preview");
        second.Should().Contain($"/media/{sha}/preview");
        brand.Downloads.Should().Be(0);
        files.Uploads.Should().Be(0);
        files.Of("editorial").Should().BeEmpty();
        (record?.FileId).Should().Be(brandFile.Id.ToString());
        (record?.FileName).Should().Be("ocharo-logo-r8.png");
        (record?.ContentType).Should().Be("image/png");
        (record?.ByteLength).Should().Be(logo.LongLength);
        files.ReferencesOf(brandFile.Id).Count.Should().Be(2);
        files.ReferencesOf(brandFile.Id).Should().Contain(new FileReference("editorial.media", sha));
        preview.StatusCode.Should().Be(HttpStatusCode.Redirect);
        preview.Headers.Location.Should().Be(TestFiles.Link(brandFile.Id, 1400));
        original.StatusCode.Should().Be(HttpStatusCode.Redirect);
        original.Headers.Location.Should().Be(TestFiles.Link(brandFile.Id, null));
    }

    [Fact]
    public async Task PublicRender_ShouldLeaveTheLogoRowAlone_WhenItAlreadyHasAFile()
    {
        var logo = BlogHarness.Png(240);
        var sha = Sha(logo);
        var files = new TestFiles();
        var brandFile = files.Add(logo, "brand", new FileReference("brand.media", sha));
        var brand =
            new LogoKit(
                new BrandSnapshot(
                    9,
                    sha,
                    "https://brand.example.test/api/public/brand/media/" + sha,
                    [],
                    LogoFileId: brandFile.Id.ToString()),
                logo);
        var host = Host(files, services: services => services.Replace(ServiceDescriptor.Singleton<IBrandKit>(brand)));
        var copied = (await Get<IMediaStore>(host).StoreAsync(logo, "ocharo-logo-r8.png", default)).ValueOrThrow();
        using var anonymous = EditorialSignIn.Anonymous(host);

        var home = await anonymous.GetStringAsync("/");
        var record = await Get<IBlogRepository>(host).FindMediaAsync(sha, default);

        home.Should().Contain($"/media/{sha}/preview");
        (record?.FileId).Should().Be(copied.FileId);
        (record?.FileId).Should().NotBe(brandFile.Id.ToString());
        files.Uploads.Should().Be(1);
        brand.Downloads.Should().Be(0);
        files.ReferencesOf(brandFile.Id).Should().NotContain(new FileReference("editorial.media", sha));
    }

    [Fact]
    public async Task PublicRender_ShouldCopyTheLogo_WhenTheBrandFileIsGoneFromFiles()
    {
        var logo = BlogHarness.Png(250);
        var sha = Sha(logo);
        var files = new TestFiles();
        var brand =
            new LogoKit(
                new BrandSnapshot(
                    10,
                    sha,
                    "https://brand.example.test/api/public/brand/media/" + sha,
                    [],
                    LogoFileId: FileId.New().ToString()),
                logo);
        var host = Host(files, services: services => services.Replace(ServiceDescriptor.Singleton<IBrandKit>(brand)));
        using var anonymous = EditorialSignIn.Anonymous(host);

        var home = await anonymous.GetStringAsync("/");
        var copy = files.Of("editorial").Should().ContainSingle().Subject;

        home.Should().Contain($"/media/{sha}/preview");
        brand.Downloads.Should().Be(1);
        copy.Sha256.Should().Be(sha);
        (await FileOfAsync(host, sha)).Should().Be(copy.Id);
    }

    [Fact]
    public async Task FilesClient_ShouldSignInAsEditorialAndExchangeTheSessionToken_WhenCalled()
    {
        var host =
            Host(
                new TestFiles(),
                settings: new Dictionary<string, string?>
                {
                    ["Identity:Client:ClientSecret"] = "editorial-test-secret"
                });

        var options = Get<IOptions<FilesClientOptions>>(host).Value;
        var subject = host.Services.GetRequiredService<IOAuth2SubjectTokenSource>();

        options.BaseAddress.Should().Be(new Uri(TestFiles.Url));
        (options.Identity?.TokenEndpoint).Should().Be(new Uri("https://identity.example.test/connect/token"));
        (options.Identity?.ClientId).Should().Be("editorial");
        // The BFF's own source (internal to the SDK) reads the session's token; the default would read a bearer header.
        subject.GetType().Name.Should().Be("BffSubjectTokenSource");
    }

    /// <summary>An Editorial host over this test's database and data folder; <paramref name="files"/> set turns WoW2
    /// Files on and stands in for it.</summary>
    private WebApplicationFactory<Program> Host(TestFiles? files, bool seed =
        false, Action<IServiceCollection>? services = null,
        IReadOnlyDictionary<string, string?>? settings = null)
    {
        var assets = Path.Combine(root, "assets");
        Directory.CreateDirectory(assets);
        byte shade = 10;
        foreach (var name in new[]
            {
                "och-logo.png",
                "och-collection.png",
                "och-collection-study.png",
                "och-nimcha.png",
                "och-cape.png"
            })
            File.WriteAllBytes(Path.Combine(assets, name), BlogHarness.Png(shade += 30));

        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            EditorialSignIn.UseBootSettings(builder);
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>(EditorialSignIn.Settings)
            {
                ["ConnectionStrings:OcharoBlog"] = $"Data Source={Path.Combine(root, "blog.db")};Pooling=False",
                ["Blog:DataRoot"] = Path.Combine(root, "data"),
                ["Blog:SeedFile"] = seed ? "SeedData/journal-seed.json" : Path.Combine(root, "missing.json"),
                ["Blog:SeedAssetRoot"] = assets,
                ["Blog:CatalogueBaseUrl"] = "",
                ["Blog:BrandBaseUrl"] = "",
                ["Blog:SchedulerSeconds"] = "0",
                ["Files:BaseUrl"] = files is null ? null : TestFiles.Url,
            }).AddInMemoryCollection(BlogDatabase.Settings(database))
                .AddInMemoryCollection(settings ?? new Dictionary<string, string?>()));
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
        FileId.TryParse((await Get<IBlogRepository>(host).FindMediaAsync(sha, default))?.FileId ?? "", out var id)
            ? id
            : default;

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
