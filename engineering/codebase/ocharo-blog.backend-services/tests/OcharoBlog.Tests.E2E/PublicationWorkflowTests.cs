using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.IO.Compression;
namespace OcharoBlog.Tests.E2E;
public sealed class PublicationWorkflowTests
{
    [Fact]
    public async Task ResearchImport_ShouldBeIdempotentAndKeepObjectsPrivate_WhenRunTwice()
    {
        await using var h=new BlogHarness();var c=h.Client();
        var response=await c.PostAsJsonAsync("/api/blog/research/import",new{});
        var result=await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(result);
        using var json=JsonDocument.Parse(result);
        (json.RootElement.GetProperty("created").GetArrayLength()>0).Should().BeTrue();
        var repeat=await c.PostAsJsonAsync("/api/blog/research/import",new{});
        var second=await repeat.Content.ReadFromJsonAsync<JsonElement>();
        second.GetProperty("created").GetArrayLength().Should().Be(0);
        var drafts=await c.GetFromJsonAsync<JsonElement>("/api/blog/articles?publication=atlas");
        drafts.EnumerateArray().Should().AllSatisfy(d=>d.GetProperty("status").GetString().Should().Be("draft"));
        ((await c.GetAsync("/atlas/textiles/bukhara-pardah-2006-152/")).StatusCode)
            .Should()
            .Be(HttpStatusCode.NotFound);
        (await c.GetStringAsync("/preview/apps/atlas/gallery/")).Should().Contain("Five widths");
        var export=await c.GetByteArrayAsync("/api/blog/export/atlas.zip");
        using var zip=new ZipArchive(new MemoryStream(export));
        zip.Entries.Should().NotContain(e=>e.FullName.Contains("bukhara-pardah"));
    }
    [Fact]
    public async Task WearGuide_ShouldChangeLivePagesAndKeepRevisions_WhenItsLifecycleMoves()
    {
        await using var h=new BlogHarness();var c=h.Client();
        var response=
            await c.PostAsJsonAsync(
                "/api/blog/articles",
                new
                {
                    slug="a-wear-guide",
                    topic="style",
                    publication="wear",
                    kind="guide",
                    title="A considered occasion",
                    excerpt="A guide",
                    intro="Start here",
                    occasions=new[]{"Everyday"},
                    sections=new[]{new{heading="Try this",paragraphs=new[]{"Try the piece with what you already own."}}}
                });
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        var draft=await response.Content.ReadFromJsonAsync<JsonElement>();
        var id=draft.GetProperty("id").GetString();
        ((await c.GetAsync("/wear/guides/a-wear-guide/")).StatusCode).Should().Be(HttpStatusCode.NotFound);
        ((await c.PostAsJsonAsync($"/api/blog/articles/{id}/publish",new{expectedRevision=1})).IsSuccessStatusCode)
            .Should()
            .BeTrue();
        (await c.GetStringAsync("/wear/guides/a-wear-guide/")).Should().Contain("A considered occasion");
        ((await c.PostAsJsonAsync($"/api/blog/articles/{id}/unpublish",new{expectedRevision=2})).IsSuccessStatusCode)
            .Should()
            .BeTrue();
        ((await c.GetAsync("/wear/guides/a-wear-guide/")).StatusCode).Should().Be(HttpStatusCode.NotFound);
        var revisions=await c.GetFromJsonAsync<JsonElement>($"/api/blog/articles/{id}/revisions");
        revisions.GetArrayLength().Should().Be(3);
    }
    [Theory][InlineData("atlas")][InlineData("wear")][InlineData("catalogue")]
    public async Task PublicAppExport_ShouldCarryItsOwnIdentity_WhenExported(string publication)
    {
        await using var h=new BlogHarness();
        var c=h.Client();
        var bytes=await c.GetByteArrayAsync($"/api/blog/export/{publication}.zip");
        using var zip=new ZipArchive(new MemoryStream(bytes));
        zip.GetEntry(publication+"/site.css").Should().NotBeNull();
        using var reader=new StreamReader(zip.GetEntry("index.html")!.Open());
        var html=await reader.ReadToEndAsync();
        html.Should().Contain("publication "+publication);
        html.Should().Contain("noindex");
    }
}
