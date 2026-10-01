namespace OcharoBlog.Application.Blog;

/// <summary>A rejected blog command; the API maps it to ProblemDetails with <see cref="StatusCode"/>.</summary>
public sealed class BlogException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static BlogException NotFound(string what) => new($"{what} was not found.", 404);

    public static BlogException Conflict(string detail) => new(detail, 409);
}
