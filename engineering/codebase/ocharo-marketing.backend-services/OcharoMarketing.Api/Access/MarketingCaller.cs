using OcharoMarketing.Application.Generations.Models;

namespace OcharoMarketing.Api.Access;

/// <summary>Reads who is calling from what <see cref="MarketingAccessMiddleware"/> published in
/// <c>HttpContext.Items</c>.</summary>
public static class MarketingCaller
{
    /// <summary>The signed-in person's subject; none for an anonymous request.</summary>
    /// <param name="http">The request.</param>
    public static string? UserId(HttpContext http)
    {
        return http.Items["Ocharo.UserId"] as string;
    }

    /// <summary>The bound organization the person was admitted to; none for an anonymous request.</summary>
    /// <param name="http">The request.</param>
    public static string? OrganizationId(HttpContext http)
    {
        return http.Items["Ocharo.OrganizationId"] as string;
    }

    /// <summary>The signed-in person the access middleware admitted, recorded on the job they ask for.</summary>
    /// <param name="http">The request.</param>
    public static JobRequester? Requester(HttpContext http)
    {
        return http.Items["Ocharo.UserId"] is string subject
            && http.Items["Ocharo.OrganizationId"] is string organization
            ? new(subject, organization)
            : null;
    }
}
