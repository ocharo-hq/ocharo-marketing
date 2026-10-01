namespace OcharoBlog.Domain.Blog;

/// <summary>Current story row: indexed scalars plus the serialized <see cref="ArticleDocument"/>.</summary>
public sealed class ArticleRecord
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = "";
    public string Topic { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTimeOffset? PublishAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public int Revision { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string Json { get; set; } = "";
}

/// <summary>An immutable snapshot of one saved story revision.</summary>
public sealed class ArticleRevisionRecord
{
    public Guid Id { get; set; }
    public Guid ArticleId { get; set; }
    public int Revision { get; set; }
    public DateTimeOffset SavedAt { get; set; }
    public string Summary { get; set; } = "";
    public string Json { get; set; } = "";
}

/// <summary>A topic row.</summary>
public sealed class TopicRecord
{
    public string Key { get; set; } = "";
    public int SortOrder { get; set; }
    public string Json { get; set; } = "";
}

/// <summary>Single-row site settings.</summary>
public sealed class SiteSettingsRecord
{
    public string Id { get; set; } = "";
    public int Revision { get; set; }
    public string Json { get; set; } = "";
}

/// <summary>Content-addressed media index.</summary>
public sealed class MediaBlobRecord
{
    public string Sha256 { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long ByteLength { get; set; }
    public string BlobPath { get; set; } = "";
    public string? PreviewPath { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTimeOffset StoredAt { get; set; }
}
