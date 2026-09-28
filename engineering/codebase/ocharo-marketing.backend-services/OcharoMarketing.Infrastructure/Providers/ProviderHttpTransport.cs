using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers;

/// <summary>Sends one bounded provider request without automatic side-effect retries.</summary>
internal static class ProviderHttpTransport
{
    private const int MaximumResponseBytes = 67_108_864;

    internal static HttpRequestMessage Create(HttpMethod method, string uri, string? token = null)
    {
        var request = new HttpRequestMessage(method, uri);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    internal static async Task<ProviderHttpReply> SendAsync(
        HttpClient client, HttpRequestMessage request, bool sideEffect, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return new(BrokerOutcome.Rejected, null, "cancelled_before_send", "Operation cancelled before dispatch.");

        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            string? requestId = response.Headers.TryGetValues("x-request-id", out var values)
                ? values.FirstOrDefault() : null;
            if (!response.IsSuccessStatusCode)
            {
                var ambiguous = sideEffect && ((int)response.StatusCode >= 500 ||
                    response.StatusCode == HttpStatusCode.RequestTimeout || (int)response.StatusCode is >= 300 and < 400);
                return new(ambiguous ? BrokerOutcome.Ambiguous : BrokerOutcome.Rejected, null,
                    "http_" + (int)response.StatusCode,
                    ambiguous ? "Provider may have accepted the operation; reconcile before retrying."
                        : "Provider rejected the request; verify access, limits, and content.",
                    requestId);
            }

            if (response.Content.Headers.ContentLength > MaximumResponseBytes)
                return InvalidSuccess(sideEffect, requestId);

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var bounded = new MemoryStream();
            var buffer = new byte[81920];
            while (true)
            {
                var count = await stream.ReadAsync(buffer, cancellationToken);
                if (count == 0) break;
                if (bounded.Length + count > MaximumResponseBytes)
                    return InvalidSuccess(sideEffect, requestId);
                await bounded.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            }

            using var document = JsonDocument.Parse(bounded.ToArray());
            return new(BrokerOutcome.Succeeded, document.RootElement.Clone(), "accepted",
                "Provider response received.", requestId);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or OperationCanceledException or JsonException or IOException)
        {
            return new(sideEffect ? BrokerOutcome.Ambiguous : BrokerOutcome.Rejected, null,
                sideEffect ? "outcome_unknown" : "read_failed",
                sideEffect ? "Provider outcome is unknown; reconcile before retrying."
                    : "Provider state could not be read.");
        }
    }

    internal static string? Text(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object &&
           element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    internal static string? Id(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
            return null;
        var text = value.ValueKind == JsonValueKind.String ? value.GetString()
            : value.ValueKind == JsonValueKind.Number ? value.GetRawText() : null;
        return !string.IsNullOrWhiteSpace(text) && text.Length <= 200 && !text.Any(char.IsControl) ? text : null;
    }

    private static ProviderHttpReply InvalidSuccess(bool sideEffect, string? requestId)
        => new(sideEffect ? BrokerOutcome.Ambiguous : BrokerOutcome.Rejected, null, "invalid_response",
            "Provider response could not establish a usable result.", requestId);
}
