using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;

namespace OcharoBlog.Tests;

public sealed class CatalogueAdminEntryTests
{
    [Theory]
    [InlineData("/catalogue/")]
    [InlineData("/catalogue/pieces/abaya/")]
    [InlineData("/catalogue/editions/review-edition/")]
    [InlineData("/catalogue/editions/review-edition/pages/cover/")]
    public async Task Explicit_https_entry_is_in_every_live_catalogue_footer(string path)
    {
        await using var h = Harness("https://platform.example/catalogue");
        var html = await h.Client().GetStringAsync(path);
        Assert.Contains("<a class=\"catalogue-admin\" href=\"https://platform.example/catalogue\" rel=\"nofollow\">Catalogue admin</a>", html);
        Assert.True(html.IndexOf("Catalogue admin", StringComparison.Ordinal) > html.IndexOf("<footer", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/catalogue")]
    [InlineData("//platform.example/catalogue")]
    [InlineData("http://platform.example/catalogue")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,admin")]
    [InlineData("https://name:password@platform.example/catalogue")]
    [InlineData("https://platform.example/catalogue#session")]
    [InlineData("https://platform.example/catalogue?token=secret")]
    [InlineData("https://platform.example/catalogue\n")]
    public async Task Missing_or_unsafe_entry_is_omitted(string? url)
    {
        await using var h = Harness(url);
        var html = await h.Client().GetStringAsync("/catalogue/");
        Assert.DoesNotContain("catalogue-admin", html);
        Assert.DoesNotContain("Catalogue admin", html);
    }

    [Theory]
    [InlineData("/atlas/")]
    [InlineData("/wear/")]
    [InlineData("/preview/apps/catalogue/")]
    public async Task Other_publications_and_editorial_preview_do_not_gain_an_admin_link(string path)
    {
        await using var h = Harness("https://platform.example/catalogue");
        var html = await h.Client().GetStringAsync(path);
        Assert.DoesNotContain("catalogue-admin", html);
    }

    [Theory]
    [InlineData("https://localhost:8340/catalogue")]
    [InlineData("https://127.0.0.1:8340/catalogue")]
    [InlineData("https://private.example/catalogue")]
    public async Task Static_export_omits_admin_entry_and_url_from_every_file(string url)
    {
        await using var h = Harness(url); var client = h.Client();
        Assert.Contains("Catalogue admin", await client.GetStringAsync("/catalogue/"));
        using var zip = new ZipArchive(new MemoryStream(await client.GetByteArrayAsync("/api/blog/export/catalogue.zip")));
        foreach (var entry in zip.Entries)
        {
            using var reader = new StreamReader(entry.Open());
            var content = await reader.ReadToEndAsync();
            Assert.DoesNotContain("Catalogue admin", content);
            Assert.DoesNotContain("catalogue-admin", content);
            Assert.DoesNotContain(url, content);
        }
        Assert.Contains("Catalogue admin", await client.GetStringAsync("/catalogue/"));
    }

    private static BlogHarness Harness(string? url) => new(seed: false, catalogueAdminUrl: url,
        services: services => services.Replace(ServiceDescriptor.Singleton<ICatalogueFeed>(new Feed())));

    private sealed class Feed : ICatalogueFeed
    {
        private static readonly CatalogueProduct Piece = new("abaya", "Abaya", "Abaya", "Cotton", null, null, null, "abaya");
        private static readonly CatalogueEdition Edition = new("review-edition", "brand", "source", 1, "Catalogue", "en", "template", 1,
            DateTimeOffset.Parse("2026-09-30T00:00:00Z"), "https://assets.example/catalogue.pdf",
            [new("cover", 1, "Cover", 210, 297, ["abaya"], "https://assets.example/cover.png", "https://assets.example/cover.pdf", "https://assets.example/cover.png")]);
        public Task<IReadOnlyList<CatalogueProduct>> GetProductsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<CatalogueProduct>>([Piece]);
        public Task<CatalogueSnapshot> GetCatalogueAsync(CancellationToken ct, string locale = "en") => Task.FromResult(new CatalogueSnapshot([Piece], Edition));
        public Task<byte[]?> DownloadAsync(string url, CancellationToken ct) => Task.FromResult<byte[]?>([1, 2, 3]);
    }
}
