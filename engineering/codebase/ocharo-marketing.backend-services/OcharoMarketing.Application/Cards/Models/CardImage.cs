namespace OcharoMarketing.Application.Cards.Models;

/// <summary>Describes the immutable image input or output of card rendering.</summary>
public sealed record CardImage(
    byte[] Bytes,
    string ContentType);
