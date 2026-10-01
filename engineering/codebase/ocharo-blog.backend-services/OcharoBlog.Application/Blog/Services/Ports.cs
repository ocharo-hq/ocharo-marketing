using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Application.Blog.Services;

/// <summary>Content-addressed image storage with a web preview per image.</summary>
public interface IMediaStore
{
    Task<MediaBlobRecord> StoreAsync(byte[] bytes, string fileName, CancellationToken ct);
    Task<MediaBlobRecord> StoreEditedAsync(byte[] bytes, string fileName, MediaEditRecord edit, CancellationToken ct);
    Task<(MediaBlobRecord Record, Stream Content)?> OpenAsync(string sha256, bool preview, CancellationToken ct);
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
