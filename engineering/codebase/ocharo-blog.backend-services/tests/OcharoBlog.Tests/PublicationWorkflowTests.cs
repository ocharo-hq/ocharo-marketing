using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.IO.Compression;
namespace OcharoBlog.Tests;
public sealed class PublicationWorkflowTests
{
    [Fact]
    public async Task Research_import_is_idempotent_and_keeps_atlas_objects_private()
    {
        await using var h=new BlogHarness();var c=h.Client();
        var response=await c.PostAsJsonAsync("/api/blog/research/import",new{});var result=await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode,result);using var json=JsonDocument.Parse(result);Assert.True(json.RootElement.GetProperty("created").GetArrayLength()>0);
        var repeat=await c.PostAsJsonAsync("/api/blog/research/import",new{});var second=await repeat.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(0,second.GetProperty("created").GetArrayLength());
        var drafts=await c.GetFromJsonAsync<JsonElement>("/api/blog/articles?publication=atlas");Assert.All(drafts.EnumerateArray(),d=>Assert.Equal("draft",d.GetProperty("status").GetString()));
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/atlas/textiles/bukhara-pardah-2006-152/")).StatusCode);
        Assert.Contains("Five widths",await c.GetStringAsync("/preview/apps/atlas/gallery/"));
        var export=await c.GetByteArrayAsync("/api/blog/export/atlas.zip");using var zip=new ZipArchive(new MemoryStream(export));Assert.DoesNotContain(zip.Entries,e=>e.FullName.Contains("bukhara-pardah"));
    }
    [Fact]
    public async Task Wear_guide_lifecycle_changes_live_pages_and_preserves_revisions()
    {
        await using var h=new BlogHarness();var c=h.Client();
        var response=await c.PostAsJsonAsync("/api/blog/articles",new{slug="a-wear-guide",topic="style",publication="wear",kind="guide",title="A considered occasion",excerpt="A guide",intro="Start here",occasions=new[]{"Everyday"},sections=new[]{new{heading="Try this",paragraphs=new[]{"Try the piece with what you already own."}}}});
        Assert.True(response.IsSuccessStatusCode,await response.Content.ReadAsStringAsync());var draft=await response.Content.ReadFromJsonAsync<JsonElement>();var id=draft.GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/wear/guides/a-wear-guide/")).StatusCode);
        Assert.True((await c.PostAsJsonAsync($"/api/blog/articles/{id}/publish",new{expectedRevision=1})).IsSuccessStatusCode);
        Assert.Contains("A considered occasion",await c.GetStringAsync("/wear/guides/a-wear-guide/"));
        Assert.True((await c.PostAsJsonAsync($"/api/blog/articles/{id}/unpublish",new{expectedRevision=2})).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/wear/guides/a-wear-guide/")).StatusCode);
        var revisions=await c.GetFromJsonAsync<JsonElement>($"/api/blog/articles/{id}/revisions");Assert.Equal(3,revisions.GetArrayLength());
    }
    [Theory][InlineData("atlas")][InlineData("wear")][InlineData("catalogue")]
    public async Task Each_public_app_exports_its_own_identity(string publication)
    {
        await using var h=new BlogHarness();var c=h.Client();var bytes=await c.GetByteArrayAsync($"/api/blog/export/{publication}.zip");using var zip=new ZipArchive(new MemoryStream(bytes));
        Assert.NotNull(zip.GetEntry(publication+"/site.css"));using var reader=new StreamReader(zip.GetEntry("index.html")!.Open());var html=await reader.ReadToEndAsync();Assert.Contains("publication "+publication,html);Assert.Contains("noindex",html);
    }
}
