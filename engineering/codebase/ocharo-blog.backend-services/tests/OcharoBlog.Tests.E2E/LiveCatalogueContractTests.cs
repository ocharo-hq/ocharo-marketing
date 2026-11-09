using System.Net.Http.Json;
using System.Text.Json;

namespace OcharoBlog.Tests.E2E;

/// <summary>Opt-in: renders catalogue pieces from a running Ocharo Catalogue (set OCHARO_CATALOGUE_URL).</summary>
public sealed class LiveCatalogueContractTests
{
    [Fact]
    public async Task Story_ShouldShowItsPieces_WhenTheLiveCatalogueHoldsThem()
    {
        var url = Environment.GetEnvironmentVariable("OCHARO_CATALOGUE_URL");
        if (string.IsNullOrWhiteSpace(url)) return;
        await using var harness = new BlogHarness(catalogueUrl: url);
        var client = harness.Client();
        var products = await client.GetFromJsonAsync<JsonElement>("/api/blog/catalogue");
        var first = products[0];
        var created = await client.PostAsJsonAsync("/api/blog/articles", new
        {
            slug = "pieces-check", topic = "pieces", title = "Pieces check", excerpt = "x", intro = "y",
            sections = new[] { new { heading = "H", paragraphs = new[] { "P" } } },
            productIds = new[] { first.GetProperty("id").GetString() },
        });
        var article = await created.Content.ReadFromJsonAsync<JsonElement>();
        await client.PostAsJsonAsync(
            $"/api/blog/articles/{article.GetProperty("id").GetString()}/publish",
            new { expectedRevision = 1 });

        var page = await client.GetStringAsync("/journal/pieces-check/");

        (products.GetArrayLength() >= 25).Should().BeTrue($"catalogue products {products.GetArrayLength()}");
        page.Should().Contain("Pieces in this story");
        page.Should().Contain(first.GetProperty("name").GetString()!);
        page.Should().Contain("UZS");
        Console.WriteLine($"LIVE PIECES: {products.GetArrayLength()} catalogue products; embedded "
            + $"{first.GetProperty("id").GetString()}");
    }
}
