namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Where one asset's bytes come from for the asset route: a short-lived WoW2 Files link to redirect to
/// (<paramref name="Location"/>), or the bytes of the local blob folder (<paramref name="Bytes"/>).</summary>
/// <param name="Asset">The asset.</param>
/// <param name="Location">The Files link, valid for <see cref="LinkLifetime"/>; none for local bytes.</param>
/// <param name="Bytes">The checked local bytes; none for a Files link.</param>
public sealed record AssetContent(AssetModel Asset, Uri? Location, byte[]? Bytes)
{
    /// <summary>How long a Files link stays valid; a cached redirect must expire before it.</summary>
    public static readonly TimeSpan LinkLifetime = TimeSpan.FromMinutes(15);
}
