namespace OcharoMarketing.Application.Cards.Models;

/// <summary>Describes the immutable preset input or output of card rendering.</summary>
public sealed record CardPreset(
    string Id,
    string Name,
    string Family,
    string Surface,
    string Description,
    bool RequiresFabric,
    int Width,
    int Height);
