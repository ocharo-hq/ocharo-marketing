using System.IO.Compression;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Application.Blog.Site;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Tests.Unit;

public sealed class ArticleFormattingTests
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
    [InlineData(null)]
    [InlineData("plain")]
    public void Render_ShouldKeepPunctuationLiteralAndEncodeHtml_WhenTheSectionIsPlain(string? format)
    {
        var html = ArticleFormatting.Render(Section("**literal** <script>alert(1)</script>", format));
        html.Should().Contain("**literal**"); html.Should().NotContain("<strong>");
        html.Should().Contain("&lt;script&gt;"); html.Should().NotContain("<script>");
    }

    [Fact]
    public void Render_ShouldFormatAndBlockExecutableContent_WhenTheSectionIsMarkdown()
    {
        var html =
            ArticleFormatting.Render(Section("## Detail\n\n**Bold** and *italic* [safe](https://example.com) "
                + "[unsafe](javascript:alert(1)).\n\n<script>alert(1)</script>\n\n<img src=x onerror=alert(1)>\n\n> "
                + "Quote\n\n- One\n- Two"));
        html.Should().Contain("<strong>Bold</strong>"); html.Should().Contain("<em>italic</em>");
        html.Should().Contain("<blockquote>"); html.Should().Contain("<ul>");
        html.Should().Contain("nofollow noopener noreferrer");
        html.Should().NotContainEquivalentOf("href=\"javascript:");
        html.Should().NotContain("<script>"); html.Should().NotContain("<img");
    }

    [Theory]
    [InlineData("[Link](https://example.com){onclick=alert(1)}")]
    [InlineData("**Words**{onmouseover=alert(1)}")]
    [InlineData("# Heading {onclick=alert(1)}")]
    public void Render_ShouldDropEventHandlers_WhenMarkdownCarriesAttributes(string text)
    {
        var html = ArticleFormatting.Render(Section(text));
        html.Should().NotContainEquivalentOf("onclick=");
        html.Should().NotContainEquivalentOf("onmouseover=");
    }

    [Theory]
    [InlineData("![Photo](https://example.com/photo.png)")]
    [InlineData("![Photo](/media/abc)")]
    [InlineData("![Photo](data:image/png;base64,abc)")]
    [InlineData("![Photo][image]\n\n[image]: https://example.com/photo.png")]
    public void Validate_ShouldRefuse_WhenMarkdownEmbedsAnImage(string text)
    {
        var error = Refusals.Of(ArticleFormatting.Validate("markdown", [text]));
        error.Message.Should().Contain("cover image editor");
    }

    [Fact]
    public void Validate_ShouldBoundFormatAndLength_WhenTheTextIsMarkdown()
    {
        Refusals.Of(ArticleFormatting.Validate("html", ["text"]));
        Refusals.Of(ArticleFormatting.Validate("markdown", [new string('a', 32001)]));
        ArticleFormatting.Validate("markdown", [new string('a', 32000)]).ThrowIfFailure();
        ArticleFormatting.Validate(null, ["![literal](https://example.com)"]).ThrowIfFailure();
        (ArticleFormatting.Text(Section("**Bold** words"))).Should().Be("Bold words");
    }
}
