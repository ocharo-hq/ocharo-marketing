using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get a static copy of a public app as a zip archive.</summary>
public sealed record PublicationExportGetQuery : IQuery<AppResult<byte[]>>
{
    /// <summary>Gets the app: <c>atlas</c>, <c>wear</c> or <c>catalogue</c>.</summary>
    public required string Publication { get; init; }

    /// <summary>Gets the locale the copy renders in.</summary>
    public required string Locale { get; init; }
}
