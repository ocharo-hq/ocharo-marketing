using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace OcharoBlog.Api.Blog;

/// <summary>Retains the public failure contract at the Marketing enquiry relay boundary.</summary>
internal static class InquiryProblemRelay
{
    private const int MaximumProblemBytes = 65_536;

    internal static async Task<ProblemDetails> ReadAsync(HttpResponseMessage upstream, HttpContext context, CancellationToken ct)
    {
        var status = (int)upstream.StatusCode;
        if (status is < 400 or > 599) status = 503;
        var problem = new ProblemDetails
        {
            Status = status, Type = "about:blank", Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = status >= 500
                ? "We could not confirm delivery. Your enquiry may already be saved. Keep your details unchanged and retry to recover the same reference."
                : "The enquiry could not be accepted. Review the supplied details and try again.",
            Instance = context.Request.Path.HasValue ? context.Request.Path.Value : "/api/public/inquiries",
        };
        CopyHeaders(upstream, context.Response);
        var mediaType = upstream.Content.Headers.ContentType?.MediaType;
        if (mediaType is not ("application/problem+json" or "application/json")) return problem;
        try
        {
            await using var stream = await upstream.Content.ReadAsStreamAsync(ct);
            using var buffer = new MemoryStream();
            var chunk = new byte[4096];
            while (buffer.Length <= MaximumProblemBytes)
            {
                var count = await stream.ReadAsync(chunk, ct);
                if (count == 0) break;
                buffer.Write(chunk, 0, count);
            }
            if (buffer.Length > MaximumProblemBytes) return problem;
            using var document = JsonDocument.Parse(buffer.ToArray());
            var body = document.RootElement;
            if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("status", out var declared)
                || declared.ValueKind != JsonValueKind.Number || !declared.TryGetInt32(out var declaredStatus) || declaredStatus != status
                || Text(body, "type", 512) is not { } type || !Uri.TryCreate(type, UriKind.RelativeOrAbsolute, out _)
                || Text(body, "title", 200) is not { } title) return problem;
            if (status < 500)
            {
                problem.Type = type;
                problem.Title = title;
                problem.Detail = Text(body, "detail", 2000) ?? problem.Detail;
                if (Errors(body) is { Count: > 0 } errors)
                {
                    problem = new ValidationProblemDetails(errors)
                    {
                        Status = status, Type = problem.Type, Title = problem.Title,
                        Detail = problem.Detail, Instance = problem.Instance,
                    };
                }
            }
            foreach (var key in new[] { "traceId", "requestId" })
                if (Text(body, key, 256) is { } id)
                    problem.Extensions[key == "traceId" ? "upstreamTraceId" : "upstreamRequestId"] = id;
            if (Text(body, "code", 128) is { } code && code.All(c => char.IsLetterOrDigit(c) || c is '_' or '-' or '.')
                && (status < 500 || code is "Unexpected" or "Unavailable" or "Timeout" or "ServiceUnavailable"))
                problem.Extensions["code"] = code;
            return problem;
        }
        catch (JsonException) { return problem; }
    }

    private static string? Text(JsonElement body, string key, int maximum)
    {
        if (!body.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.String) return null;
        var text = value.GetString();
        return !string.IsNullOrWhiteSpace(text) && text.Length <= maximum && !text.Any(char.IsControl) ? text : null;
    }

    private static Dictionary<string, string[]>? Errors(JsonElement body)
    {
        if (!body.TryGetProperty("errors", out var fields) || fields.ValueKind != JsonValueKind.Object) return null;
        var errors = new Dictionary<string, string[]>();
        foreach (var field in fields.EnumerateObject())
        {
            if (errors.Count >= 50 || field.Name.Length > 128 || field.Name.Any(char.IsControl)
                || field.Value.ValueKind != JsonValueKind.Array) return null;
            var values = field.Value.EnumerateArray().Take(11).ToArray();
            if (values.Length > 10 || values.Any(v => v.ValueKind != JsonValueKind.String || v.GetString() is not { Length: <= 1000 } message || message.Any(char.IsControl)))
                return null;
            errors[field.Name] = values.Select(v => v.GetString()!).ToArray();
        }
        return errors;
    }

    private static void CopyHeaders(HttpResponseMessage upstream, HttpResponse response)
    {
        if (upstream.Headers.TryGetValues("Retry-After", out var retries))
            foreach (var retry in retries.Take(1))
                if (RetryConditionHeaderValue.TryParse(retry, out var parsed)) response.Headers.RetryAfter = parsed.ToString();
        if (upstream.Headers.TryGetValues("WWW-Authenticate", out var challenges))
            foreach (var challenge in challenges.Take(8))
                if (challenge.Length <= 2048 && !challenge.Any(char.IsControl)
                    && AuthenticationHeaderValue.TryParse(challenge, out var parsed))
                    response.Headers.Append("WWW-Authenticate", parsed.ToString());
        var values = upstream.Headers.TryGetValues("Allow", out var allowed) ? allowed
            : upstream.Content.Headers.TryGetValues("Allow", out allowed) ? allowed : [];
        var methods = values.SelectMany(value => value.Split(',')).Select(value => value.Trim()).Take(32)
            .Where(value => value.Length is > 0 and <= 32 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'));
        var allow = string.Join(", ", methods);
        if (allow.Length > 0) response.Headers.Allow = allow;
    }
}
