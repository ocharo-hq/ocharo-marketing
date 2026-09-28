namespace OcharoMarketing.Application.Cards.Models;

/// <summary>Describes the immutable preview input or output of card rendering.</summary>
public sealed record CardPreview(
    string Html,
    IReadOnlyList<string> Issues);
