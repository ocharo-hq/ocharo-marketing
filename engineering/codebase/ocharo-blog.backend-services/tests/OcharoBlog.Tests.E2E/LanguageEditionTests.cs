using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;

namespace OcharoBlog.Tests.E2E;

/// <summary>Uzbek/Russian editions: translation drafts, /uz/ routes, interface strings, hreflang, feeds and
/// export.</summary>
public sealed class LanguageEditionTests
{
    [Fact]
    public async Task Translation_ShouldGetLocalizedPagesAndAlternates_WhenPublished()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();
        var list = await client.GetFromJsonAsync<JsonElement>("/api/blog/articles?q=the-nimcha-in-a-new-light");
        var sourceId = list[0].GetProperty("id").GetString();

        var beforeUz = await client.GetAsync("/uz/");
        var translated =
            await client.PostAsJsonAsync($"/api/blog/articles/{sourceId}/translate", new { language = "uz" });
        var edition = await translated.Content.ReadFromJsonAsync<JsonElement>();
        var duplicate =
            await client.PostAsJsonAsync($"/api/blog/articles/{sourceId}/translate", new { language = "uz" });
        var editionId = edition.GetProperty("id").GetString();
        var draft =
            JsonSerializer.Deserialize<Dictionary<string, object?>>(edition.GetProperty("document").GetRawText())!;
        draft["title"] = "Nega biz kiymaydigan kiyimlarni yaxshi ko‘ramiz?";
        draft["expectedRevision"] = 1;
        var saved = await client.PutAsJsonAsync($"/api/blog/articles/{editionId}", draft);
        var published =
            await client.PostAsJsonAsync($"/api/blog/articles/{editionId}/publish", new { expectedRevision = 2 });

        var uzHome = await client.GetStringAsync("/uz/");
        var uzStory = await client.GetStringAsync("/uz/journal/the-nimcha-in-a-new-light-uz/");
        var enStory = await client.GetStringAsync("/journal/the-nimcha-in-a-new-light/");
        var enStoryNotInUz = await client.GetAsync("/uz/journal/let-one-piece-lead/");
        var uzFeed = XDocument.Parse(await client.GetStringAsync("/uz/feed.xml"));
        var sitemap = await client.GetStringAsync("/sitemap.xml");
        var ruHome = await client.GetAsync("/ru/");

        beforeUz.StatusCode.Should().Be(HttpStatusCode.NotFound);
        edition.GetProperty("document").GetProperty("language").GetString().Should().Be("uz");
        edition.GetProperty("document")
            .GetProperty("translationKey")
            .GetString()
            .Should()
            .Be("the-nimcha-in-a-new-light");
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        saved.StatusCode.Should().Be(HttpStatusCode.OK);
        published.StatusCode.Should().Be(HttpStatusCode.OK);
        uzHome.Should().Contain("<html lang=\"uz\">");
        uzHome.Should().Contain("Barcha hikoyalar");
        uzStory.Should().Contain("Nega biz kiymaydigan kiyimlarni yaxshi ko‘ramiz?");
        uzStory.Should().Contain("hreflang=\"en\"");
        enStory.Should().Contain("href=\"/uz/journal/the-nimcha-in-a-new-light-uz/\" hreflang=\"uz\"");
        enStoryNotInUz.StatusCode.Should().Be(HttpStatusCode.NotFound);
        uzFeed.Descendants("item").Should().ContainSingle();
        sitemap.Should().Contain("/uz/journal/the-nimcha-in-a-new-light-uz/");
        sitemap.Should().Contain("/uz/topics/pieces/");
        ruHome.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var zip = new ZipArchive(new MemoryStream(await client.GetByteArrayAsync("/api/blog/export/site.zip")));
        zip.Entries.Should().Contain(e => e.FullName == "uz/journal/the-nimcha-in-a-new-light-uz/index.html");
        zip.Entries.Should().Contain(e => e.FullName == "uz/feed.xml");
    }

    [Fact]
    public async Task TopicTranslations_ShouldLabelTheNavigation_WhenTheEditionIsUzbek()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();
        var topics = await client.GetFromJsonAsync<JsonElement>("/api/blog/topics");
        var pieces = topics.EnumerateArray().First(t => t.GetProperty("key").GetString() == "pieces");
        var saved = await client.PutAsJsonAsync("/api/blog/topics/pieces", new
        {
            key =
                "pieces", name =
                    pieces.GetProperty("name").GetString(), @short = pieces.GetProperty("short").GetString(),
            description = pieces.GetProperty("description").GetString(), sortOrder = 0,
            translations = new Dictionary<string, object>
            {
                ["uz"] = new
                {
                    name = "Kiyim va kundalik hayot",
                    @short = "Kundalik",
                    description = "Kiyim javoni savollari."
                }
            },
        });
        var list = await client.GetFromJsonAsync<JsonElement>("/api/blog/articles?q=let-one-piece-lead");
        var translated =
            await client.PostAsJsonAsync(
                $"/api/blog/articles/{list[0].GetProperty("id").GetString()}/translate",
                new { language = "uz" });
        var editionId = (await translated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        await client.PostAsJsonAsync($"/api/blog/articles/{editionId}/publish", new { expectedRevision = 1 });

        var uzTopic = await client.GetStringAsync("/uz/topics/pieces/");
        var enTopic = await client.GetStringAsync("/topics/pieces/");

        saved.StatusCode.Should().Be(HttpStatusCode.OK);
        uzTopic.Should().Contain("Kiyim va kundalik hayot");
        uzTopic.Should().Contain(">Kundalik<");
        enTopic.Should().NotContain("Kiyim va kundalik hayot");
    }
}
