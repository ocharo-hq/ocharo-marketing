using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;

namespace OcharoBlog.Tests.E2E;

public sealed class CatalogueAdminEntryTests
{
    [Theory]
    [InlineData("/catalogue/")]
    [InlineData("/catalogue/pieces/abaya/")]
    [InlineData("/catalogue/editions/review-edition/")]
    [InlineData("/catalogue/editions/review-edition/pages/cover/")]
    public async Task CatalogueFooter_ShouldLinkTheAdminEntry_WhenItIsAnHttpsAddress(string path)
    {
        await using var h = Harness("https://platform.example/catalogue");
        var html = await h.Client().GetStringAsync(path);
        html.Should()
            .Contain("<a class=\"catalogue-admin\" href=\"https://platform.example/catalogue\" rel=\"nofollow\">"
                + "Catalogue admin</a>");
        (html.IndexOf("Catalogue admin", StringComparison.Ordinal) > html.IndexOf("<footer", StringComparison.Ordinal))
            .Should()
            .BeTrue();
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
    public async Task CatalogueFooter_ShouldOmitTheAdminEntry_WhenItIsMissingOrUnsafe(string? url)
    {
        await using var h = Harness(url);
        var html = await h.Client().GetStringAsync("/catalogue/");
        html.Should().NotContain("catalogue-admin");
        html.Should().NotContain("Catalogue admin");
    }

    [Theory]
    [InlineData("/atlas/")]
    [InlineData("/wear/")]
    [InlineData("/preview/apps/catalogue/")]
    public async Task AdminEntry_ShouldStayOffOtherApps_WhenTheCatalogueHasOne(string path)
    {
        await using var h = Harness("https://platform.example/catalogue");
        var html = await h.Client().GetStringAsync(path);
        html.Should().NotContain("catalogue-admin");
    }

    [Theory]
    [InlineData("https://localhost:8340/catalogue")]
    [InlineData("https://127.0.0.1:8340/catalogue")]
    [InlineData("https://private.example/catalogue")]
    public async Task CatalogueExport_ShouldOmitTheAdminEntry_WhenOneIsConfigured(string url)
    {
        await using var h = Harness(url); var client = h.Client();
        (await client.GetStringAsync("/catalogue/")).Should().Contain("Catalogue admin");
        using var zip =
            new ZipArchive(new MemoryStream(await client.GetByteArrayAsync("/api/blog/export/catalogue.zip")));
        foreach (var entry in zip.Entries)
        {
            using var reader = new StreamReader(entry.Open());
            var content = await reader.ReadToEndAsync();
            content.Should().NotContain("Catalogue admin");
            content.Should().NotContain("catalogue-admin");
            content.Should().NotContain(url);
        }
        (await client.GetStringAsync("/catalogue/")).Should().Contain("Catalogue admin");
    }

    private static BlogHarness Harness(string? url) => new(seed: false, catalogueAdminUrl: url,
        services: services => services.Replace(ServiceDescriptor.Singleton<ICatalogueFeed>(new Feed())));

    private sealed class Feed : ICatalogueFeed
    {
        private static readonly CatalogueProduct Piece =
            new("abaya", "Abaya", "Abaya", "Cotton", null, null, null, "abaya");
        private static readonly CatalogueEdition Edition =
            new("review-edition", "brand", "source", 1, "Catalogue", "en", "template", 1,
            DateTimeOffset.Parse("2026-09-30T00:00:00Z"), "https://assets.example/catalogue.pdf",
            [new(
                "cover",
                1,
                "Cover",
                210,
                297,
                ["abaya"],
                "https://assets.example/cover.png",
                "https://assets.example/cover.pdf",
                "https://assets.example/cover.png")]);
        public Task<IReadOnlyList<CatalogueProduct>> GetProductsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CatalogueProduct>>([Piece]);
        public Task<CatalogueSnapshot> GetCatalogueAsync(CancellationToken ct, string locale = "en") =>
            Task.FromResult(new CatalogueSnapshot([Piece], Edition));
        public Task<byte[]?> DownloadAsync(string url, CancellationToken ct) => Task.FromResult<byte[]?>([1, 2, 3]);
    }
}
