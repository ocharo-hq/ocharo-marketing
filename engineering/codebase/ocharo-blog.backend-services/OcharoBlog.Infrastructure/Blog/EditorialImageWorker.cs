using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Identity.Core.Organizations;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Asks Desk to remove backgrounds and checks a saved edit against the current session.</summary>
/// <remarks>Editorial calls Desk as its own client: <see cref="ClientName"/> carries a service token and no person travels on
/// the call. The access middleware has checked the person's <c>editorial:edit</c> by then.</remarks>
public sealed class EditorialImageWorker(IOptions<BlogOptions> options, IHttpContextAccessor accessor, IHttpClientFactory clients, ILogger<EditorialImageWorker> logger) : IEditorialImageWorker
{
    /// <summary>The HTTP client that reaches Desk.</summary>
    public const string ClientName = "ocharo-image-processing";

    private const string CapabilitiesPath = "/api/integrations/media/background-removal/capabilities";
    private const string RemovalPath = "/api/integrations/media/background-removal";

    /// <summary>How long a removal may take, its response body included.</summary>
    public static readonly TimeSpan RemovalDeadline = TimeSpan.FromSeconds(95);

    private static readonly TimeSpan StatusDeadline = TimeSpan.FromSeconds(5);

    public async Task<ImageWorkerStatus> GetStatusAsync(CancellationToken ct)
    {
        if (DeskOrigin(options.Value.DeskBaseUrl) is not { } desk) return new(false);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(StatusDeadline);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(desk, CapabilitiesPath));
            using var response = await clients.CreateClient(ClientName).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (IsRefused(response) || !response.IsSuccessStatusCode) return new(false);
            var bytes = await ReadAsync(response.Content, 4096, deadline.Token);
            return JsonSerializer.Deserialize<ImageWorkerStatus>(bytes, BlogJson.Options) ?? new(false);
        }
        catch (Exception e) when (e is BlogException || IsCallFailure(e, ct))
        {
            logger.LogWarning("Could not read Desk's background removal status: {Failure}", e.Message);
            return new(false);
        }
    }

    public async Task<(byte[] Bytes, string Model)> RemoveAsync(byte[] bytes, string contentType, CancellationToken ct)
    {
        if (DeskOrigin(options.Value.DeskBaseUrl) is not { } desk) throw Unavailable();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(RemovalDeadline);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(desk, RemovalPath)) { Content = new ByteArrayContent(bytes) };
            request.Content.Headers.ContentType = new(contentType);
            using var response = await clients.CreateClient(ClientName).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (IsRefused(response)) throw Unavailable();
            if (response.StatusCode == HttpStatusCode.TooManyRequests) throw new BlogException("Image processing is busy. Try again shortly.", 429);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType != "image/png") throw Unavailable();
            var models = response.Headers.TryGetValues("X-Image-Model", out var values) ? values.ToArray() : [];
            if (models.Length != 1 || models[0].Length is 0 or > 100 || !models[0].All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')) throw Unavailable();
            return (await ReadAsync(response.Content, MediaEditingService.MaximumBytes, deadline.Token), models[0]);
        }
        catch (Exception e) when (IsCallFailure(e, ct))
        {
            logger.LogWarning("Background removal through Desk failed: {Failure}", e.Message);
            throw Unavailable();
        }
    }

    public Task EnsureEditableAsync(MediaEditActor actor, CancellationToken ct)
    {
        var organization = options.Value.OrganizationId;
        if (accessor.HttpContext?.User is not { } user
            || !EditorialIdentity.IsMember(user, organization)
            || !Guid.TryParse(actor.OrganizationId, out var actorOrganization) || actorOrganization != organization
            || user.FindFirst(EditorialIdentity.SubjectClaim)?.Value != actor.UserId)
            throw new BlogException("Your connection expired. Your original image is unchanged.", 401);
        if (!user.HasOrganizationPermission(organization, EditorialIdentity.EditPermission))
            throw new BlogException("Editor access is required.", 403);
        return Task.CompletedTask;
    }

    /// <summary>Reads Desk's origin from <c>Blog:DeskBaseUrl</c>: HTTPS with no credentials, path, query or fragment; any other value names none.</summary>
    public static Uri? DeskOrigin(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.AbsoluteUri == $"https://{uri.Authority}/" ? uri : null;

    /// <summary>Whether Desk refused the service token: the client's registration is wrong, whoever is signed in.</summary>
    private bool IsRefused(HttpResponseMessage response)
    {
        if (response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)) return false;
        logger.LogWarning("Desk refused Editorial's service token with {Status}; check the client's registration for Desk.", (int)response.StatusCode);
        return true;
    }

    /// <summary>Whether a call failed on the way to Desk or at the token endpoint: a refused request is an
    /// <see cref="HttpRequestException"/>, a token response without a token an <see cref="AppException"/> or a
    /// <see cref="JsonException"/>, an unset client an <see cref="InvalidOperationException"/>.</summary>
    private static bool IsCallFailure(Exception e, CancellationToken ct) =>
        e is HttpRequestException or AppException or JsonException or InvalidOperationException
        || e is OperationCanceledException && !ct.IsCancellationRequested;

    private static async Task<byte[]> ReadAsync(HttpContent content, int maximum, CancellationToken ct)
    {
        if (content.Headers.ContentLength > maximum) throw Unavailable();
        await using var stream = await content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + count > maximum) throw Unavailable();
            buffer.Write(chunk, 0, count);
        }
        return buffer.ToArray();
    }

    private static BlogException Unavailable() => new("Background removal is unavailable. Your original image is unchanged.", 503);
}
