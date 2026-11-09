using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get every story that matches the editor's filters, newest first.</summary>
public sealed record ArticleGetAllQuery : IQuery<AppResult<IReadOnlyList<ArticleSummary>>>
{
    /// <summary>Gets the status a story must have; none for any.</summary>
    public string? Status { get; init; }

    /// <summary>Gets the topic a story must belong to; none for any.</summary>
    public string? Topic { get; init; }

    /// <summary>Gets the text a story's title, slug or excerpt must contain; none for any.</summary>
    public string? Search { get; init; }

    /// <summary>Gets the language a story must be in; none for any.</summary>
    public string? Language { get; init; }

    /// <summary>Gets the publication a story must belong to; none for any.</summary>
    public string? Publication { get; init; }
}
