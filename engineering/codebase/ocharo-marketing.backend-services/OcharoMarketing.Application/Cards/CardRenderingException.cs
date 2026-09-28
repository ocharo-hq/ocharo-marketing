namespace OcharoMarketing.Application.Cards;

/// <summary>Reports a controlled, safe-to-display card preparation failure.</summary>
public sealed class CardRenderingException(string message) : Exception(message);
