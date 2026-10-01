using System.IO.Compression;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Application.Blog.Site;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Tests;

public sealed class ArticleFormattingTests
{
    private static ArticleSection Section(string text, string? format = "markdown") => new("Story section", [text], null, null, format);
    private static ArticleInput Story(string publication = "blog") => new()
    {
        Slug = "formatted-" + publication, Topic = "style", Title = "Formatted story", Excerpt = "A useful story.", Intro = "Introduction.",
        Publication = publication, Kind = publication == "wear" ? "guide" : "story", Occasions = publication == "wear" ? ["Everyday"] : [],
        Sections = [Section("**Silk** and *cotton*.\n\n> A closer look.\n\n- First detail\n- Second detail\n\n[Source](https://example.com)")],
        Sources = [new("Source", "https://example.com", null)],
    };

    [Theory]
    [InlineData(null)]
    [InlineData("plain")]
    public void Existing_plain_sections_keep_literal_punctuation_and_html(string? format)
    {
        var html = ArticleFormatting.Render(Section("**literal** <script>alert(1)</script>", format));
        Assert.Contains("**literal**", html); Assert.DoesNotContain("<strong>", html);
        Assert.Contains("&lt;script&gt;", html); Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void Explicit_markdown_supports_formatting_and_blocks_executable_html_or_links()
    {
        var html = ArticleFormatting.Render(Section("## Detail\n\n**Bold** and *italic* [safe](https://example.com) [unsafe](javascript:alert(1)).\n\n<script>alert(1)</script>\n\n<img src=x onerror=alert(1)>\n\n> Quote\n\n- One\n- Two"));
        Assert.Contains("<strong>Bold</strong>", html); Assert.Contains("<em>italic</em>", html);
        Assert.Contains("<blockquote>", html); Assert.Contains("<ul>", html);
        Assert.Contains("nofollow noopener noreferrer", html);
        Assert.DoesNotContain("href=\"javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script>", html); Assert.DoesNotContain("<img", html);
    }

    [Theory]
    [InlineData("[Link](https://example.com){onclick=alert(1)}")]
    [InlineData("**Words**{onmouseover=alert(1)}")]
    [InlineData("# Heading {onclick=alert(1)}")]
    public void Extended_markdown_attributes_cannot_create_event_handlers(string text)
    {
        var html = ArticleFormatting.Render(Section(text));
        Assert.DoesNotContain("onclick=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onmouseover=", html, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("![Photo](https://example.com/photo.png)")]
    [InlineData("![Photo](/media/abc)")]
    [InlineData("![Photo](data:image/png;base64,abc)")]
    [InlineData("![Photo][image]\n\n[image]: https://example.com/photo.png")]
    public void Inline_images_cannot_bypass_the_media_workflow(string text)
    {
        var error = Assert.Throws<BlogException>(() => ArticleFormatting.Validate("markdown", [text]));
        Assert.Contains("cover image editor", error.Message);
    }

    [Fact]
    public void Format_and_length_are_bounded_but_plain_stars_are_untouched()
    {
        Assert.Throws<BlogException>(() => ArticleFormatting.Validate("html", ["text"]));
        Assert.Throws<BlogException>(() => ArticleFormatting.Validate("markdown", [new string('a', 32001)]));
        ArticleFormatting.Validate("markdown", [new string('a', 32000)]);
        ArticleFormatting.Validate(null, ["![literal](https://example.com)"]);
        Assert.Equal("Bold words", ArticleFormatting.Text(Section("**Bold** words")));
    }

    [Theory]
    [InlineData("blog")]
    [InlineData("atlas")]
    [InlineData("wear")]
    public async Task Formatting_survives_save_revision_preview_publication_and_static_export(string publication)
    {
        await using var harness = new BlogHarness(); _ = harness.Client();
        var articles = harness.Get<ArticleService>(); var input = Story(publication);
        var saved = await articles.CreateAsync(input, CancellationToken.None);
        Assert.Equal("markdown", saved.Document.Sections[0].Format);
        Assert.Contains("\n\n>", saved.Document.Sections[0].Paragraphs[0]);
        var preview = publication == "blog"
            ? await harness.Get<SiteService>().PreviewAsync(saved.Id, CancellationToken.None)
            : await harness.Get<PublicationSiteService>().PreviewAsync(saved.Id, CancellationToken.None);
        Assert.Contains("<strong>Silk</strong>", preview);
        Assert.Contains("noindex", preview);
        await articles.PublishAsync(saved.Id, saved.Revision, CancellationToken.None);
        var bytes = publication == "blog"
            ? await harness.Get<SiteService>().ExportAsync(CancellationToken.None)
            : await harness.Get<PublicationSiteService>().ExportAsync(publication, CancellationToken.None);
        using var archive = new ZipArchive(new MemoryStream(bytes));
        var path = publication switch { "atlas" => "atlas/stories/formatted-atlas/index.html", "wear" => "wear/guides/formatted-wear/index.html", _ => "journal/formatted-blog/index.html" };
        using var reader = new StreamReader(archive.GetEntry(path)!.Open());
        var html = await reader.ReadToEndAsync(); Assert.Contains("<strong>Silk</strong>", html); Assert.Contains("<blockquote>", html);
    }

    [Fact]
    public async Task Markdown_save_retains_indentation_and_rejects_images_and_unknown_formats()
    {
        await using var harness = new BlogHarness(); _ = harness.Client(); var articles = harness.Get<ArticleService>();
        var saved = await articles.CreateAsync(Story() with { Sections = [Section("    code\n\nText  \nline")] }, CancellationToken.None);
        Assert.StartsWith("    code", saved.Document.Sections[0].Paragraphs[0]);
        Assert.Contains("Text  \n", saved.Document.Sections[0].Paragraphs[0]);
        await Assert.ThrowsAsync<BlogException>(() => articles.CreateAsync(Story() with { Slug = "image-embed", Sections = [Section("![photo](https://example.com/a.png)")] }, CancellationToken.None));
        await Assert.ThrowsAsync<BlogException>(() => articles.CreateAsync(Story() with { Slug = "html-format", Sections = [Section("words", "html")] }, CancellationToken.None));
    }
}
