namespace OcharoBlog.Application.Blog.Models;

/// <summary>The drafts an import of the editorial research pack created, and the slugs it left as they were.</summary>
public sealed record StarterImportResult(IReadOnlyList<Guid> Created, IReadOnlyList<string> Skipped);
