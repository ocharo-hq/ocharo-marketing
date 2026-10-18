using System.Net;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Options;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Media.Markdown;

namespace OcharoBlog.Application.Blog.Services;

/// <summary>Renders explicitly formatted sections under a fixed policy; older plain text stays literal.</summary>
public static class ArticleFormatting
{
    public const int MaximumMarkdownCharacters = 32000;
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseYamlFrontMatter().DisableHtml().Build();
    private static readonly IMarkdownService Markdown = new MarkdigMarkdownService(new SafeOptions());

    /// <summary>Rejects unsupported formats and images that bypass the reviewed media workflow.</summary>
    public static void Validate(string? format, IReadOnlyList<string> paragraphs)
    {
        if (format is not (null or "plain" or "markdown")) throw new BlogException("Section format must be plain or markdown.");
        if (format != "markdown") return;
        var source = string.Join("\n\n", paragraphs);
        if (source.Length > MaximumMarkdownCharacters) throw new BlogException($"Formatted text is longer than {MaximumMarkdownCharacters} characters.");
        if (Markdig.Markdown.Parse(source, Pipeline).Descendants<LinkInline>().Any(link => link.IsImage))
            throw new BlogException("Inline Markdown images are not supported. Use the cover image editor and media library.");
    }

    /// <summary>Uses the same safe rendering for Blog, Atlas, Wear, previews and static exports.</summary>
    public static string Render(ArticleSection section)
    {
        if (section.Format != "markdown") return string.Concat(section.Paragraphs.Select(value => $"<p>{WebUtility.HtmlEncode(value)}</p>"));
        Validate(section.Format, section.Paragraphs);
        return "<div class=\"formatted-section\">" + Markdown.Render(string.Join("\n\n", section.Paragraphs)).Html + "</div>";
    }

    /// <summary>Counts visible text without treating Markdown punctuation as words.</summary>
    public static string Text(ArticleSection section) => section.Format == "markdown"
        ? Markdown.ToPlainText(string.Join("\n\n", section.Paragraphs))
        : string.Join("\n\n", section.Paragraphs);

    /// <summary>Author-provided HTML cannot be enabled by host configuration.</summary>
    private sealed class SafeOptions : IOptionsMonitor<MarkdownOptions>
    {
        public MarkdownOptions CurrentValue => new() { AllowRawHtml = false };
        public MarkdownOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<MarkdownOptions, string?> listener) => null;
    }
}
