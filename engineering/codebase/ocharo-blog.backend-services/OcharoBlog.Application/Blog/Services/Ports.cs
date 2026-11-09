using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Application.Blog.Services;

/// <summary>Content-addressed image storage with a web preview per image.</summary>
public interface IMediaStore
{
    /// <summary>Stores bytes once per SHA-256 (idempotent) and returns the index row.</summary>
    Task<MediaBlobRecord> StoreAsync(byte[] bytes, string fileName, CancellationToken ct);

    /// <summary>Stores an edited derivative once per SHA-256 together with its immutable receipt.</summary>
    Task<MediaBlobRecord> StoreEditedAsync(byte[] bytes, string fileName, MediaEditRecord edit, CancellationToken ct);

    /// <summary>Opens a stored file for a media route: a Files link to redirect to, or local bytes; null when unknown.</summary>
    Task<MediaContent?> OpenAsync(string sha256, bool preview, CancellationToken ct);

    /// <summary>Reads a stored file's bytes on the server, for editing and static exports: the original, or its WebP
    /// preview; null when unknown or gone.</summary>
    Task<(MediaBlobRecord Record, Stream Content)?> ReadAsync(string sha256, bool preview, CancellationToken ct);
}

/// <summary>Where one stored file's bytes come from: a short-lived WoW2 Files link to redirect to
/// (<paramref name="Location"/>), or a stream from the local blob folder (<paramref name="Content"/>).</summary>
/// <param name="Record">The stored file.</param>
/// <param name="ContentType">The type the bytes carry: the original's, or WebP for a preview.</param>
/// <param name="Location">The Files link, valid for <see cref="LinkLifetime"/>; none for local bytes.</param>
/// <param name="Content">The local bytes; none for a Files link.</param>
public sealed record MediaContent(MediaBlobRecord Record, string ContentType, Uri? Location, Stream? Content)
{
    /// <summary>How long a Files link stays valid; a cached redirect must expire before it.</summary>
    public static readonly TimeSpan LinkLifetime = TimeSpan.FromMinutes(15);
}

/// <summary>Whether a stored image has a web preview.</summary>
public static class MediaPreview
{
    /// <summary>A local file has one once its preview was written; a file in WoW2 Files always has one, as a variant.</summary>
    public static bool Exists(MediaBlobRecord record) => record.PreviewPath is not null || record.FileId is not null;
}

/// <summary>Read-only access to the Ocharo Catalogue public feed; an unavailable catalogue yields an empty list.</summary>
public interface ICatalogueFeed
{
    Task<IReadOnlyList<CatalogueProduct>> GetProductsAsync(CancellationToken ct);

    /// <summary>One release-consistent read, including an optional fixed-page edition.</summary>
    Task<CatalogueSnapshot> GetCatalogueAsync(CancellationToken ct, string locale = "en");

    /// <summary>Downloads a catalogue image (for static export); null when unavailable.</summary>
    Task<byte[]?> DownloadAsync(string url, CancellationToken ct);
}

/// <summary>The published Ocharo brand kit as the blog uses it: the on-light logo file and the public links.</summary>
public sealed record BrandSnapshot(int Release, string? LogoSha256, string? LogoUrl, IReadOnlyList<SiteLink> Links, BrandEvent? Event = null);

/// <summary>An active brand appearance's event badge: shown beside the logo on live pages until <see cref="EndsAt"/>.</summary>
public sealed record BrandEvent(string Name, string Label, string Url, DateTimeOffset EndsAt);

/// <summary>Read-only access to the Ocharo Brand service; null when it is not configured or unreachable.</summary>
public interface IBrandKit
{
    Task<BrandSnapshot?> GetAsync(CancellationToken ct);

    /// <summary>Downloads a brand file (the logo is copied into the blog's media store); null when unavailable.</summary>
    Task<byte[]?> DownloadAsync(string url, CancellationToken ct);
}
