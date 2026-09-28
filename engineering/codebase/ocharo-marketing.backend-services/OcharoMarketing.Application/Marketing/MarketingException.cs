namespace OcharoMarketing.Application.Marketing;

/// <summary>Signals a rejected marketing command with its HTTP-safe explanation.</summary>
public sealed class MarketingException(string message, int statusCode = 400) : Exception(message)
{
    /// <summary>Gets the response status for this rejected command.</summary>
    public int StatusCode { get; } = statusCode;
}
