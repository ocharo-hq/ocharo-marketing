using System.IO.Compression;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Application.Blog.Site;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Tests.Integration;

public sealed class ArticleFormattingStorageTests
{
    private static ArticleSection Section(string text, string? format = "markdown") =>
        new("Story section", [text], null, null, format);
    private static ArticleInput Story(string publication = "blog") => new()
    {
        Slug =
            "formatted-" + publication, Topic =
                "style", Title = "Formatted story", Excerpt = "A useful story.", Intro = "Introduction.",
        Publication =
            publication, Kind =
                publication == "wear" ? "guide" : "story", Occasions = publication == "wear" ? ["Everyday"] : [],
        Sections =
            [Section("**Silk** and *cotton*.\n\n> A closer look.\n\n- First detail\n- Second "
                + "detail\n\n[Source](https://example.com)")],
        Sources = [new("Source", "https://example.com", null)],
    };

    [Theory]
    [InlineData("blog")]
    [InlineData("atlas")]
    [InlineData("wear")]
    public async Task Formatting_ShouldSurviveEveryStage_WhenAStoryIsPublishedAndExported(string publication)
    {
        await using var harness = new BlogHarness(); _ = harness.Client();
        var articles = harness.Get<ArticleService>(); var input = Story(publication);
        var saved = (await articles.CreateAsync(input, CancellationToken.None)).ValueOrThrow();
        saved.Document.Sections[0].Format.Should().Be("markdown");
        saved.Document.Sections[0].Paragraphs[0].Should().Contain("\n\n>");
        var preview = publication == "blog"
            ? (await harness.Get<SiteService>().PreviewAsync(saved.Id, CancellationToken.None)).ValueOrThrow()
            : (await harness.Get<PublicationSiteService>().PreviewAsync(saved.Id, CancellationToken.None))
                .ValueOrThrow();
        preview.Should().Contain("<strong>Silk</strong>");
        preview.Should().Contain("noindex");
        (await articles.PublishAsync(saved.Id, saved.Revision, CancellationToken.None)).ValueOrThrow();
        var bytes = publication == "blog"
            ? await harness.Get<SiteService>().ExportAsync(CancellationToken.None)
            : (await harness.Get<PublicationSiteService>().ExportAsync(publication, CancellationToken.None))
                .ValueOrThrow();
        using var archive = new ZipArchive(new MemoryStream(bytes));
        var path = publication switch
        {
            "atlas" => "atlas/stories/formatted-atlas/index.html",
            "wear" => "wear/guides/formatted-wear/index.html",
            _ => "journal/formatted-blog/index.html"
        };
        using var reader = new StreamReader(archive.GetEntry(path)!.Open());
        var html = await reader.ReadToEndAsync();
        html.Should().Contain("<strong>Silk</strong>");
        html.Should().Contain("<blockquote>");
    }

    [Fact]
    public async Task MarkdownSave_ShouldKeepIndentationAndRefuseImages_WhenSaved()
    {
        await using var harness = new BlogHarness(); _ = harness.Client(); var articles = harness.Get<ArticleService>();
        var saved =
            (await articles.CreateAsync(
                Story() with { Sections = [Section("    code\n\nText  \nline")] },
                CancellationToken.None))
                .ValueOrThrow();
        saved.Document.Sections[0].Paragraphs[0].Should().StartWith("    code");
        saved.Document.Sections[0].Paragraphs[0].Should().Contain("Text  \n");
        Refusals.Of(await articles.CreateAsync(
            Story() with { Slug = "image-embed", Sections = [Section("![photo](https://example.com/a.png)")] },
            CancellationToken.None));
        Refusals.Of(await articles.CreateAsync(
            Story() with { Slug = "html-format", Sections = [Section("words", "html")] },
            CancellationToken.None));
    }
}
