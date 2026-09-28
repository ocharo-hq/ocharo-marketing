using System.Net.Http.Json;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers.Brokers;

/// <summary>Integrates WhatsApp Business Cloud customer messaging.</summary>
/// <remarks>Caller-confirmed permission and service windows are required; acceptance does not establish delivery.</remarks>
public sealed class WhatsAppSocialPublishingBroker(HttpClient client, WhatsAppProviderOptions options) : ISocialPublishingBroker
{
    public string Provider => "whatsapp";
    public IReadOnlySet<string> Capabilities { get; } = new HashSet<string> { "template-message", "service-text", "service-photo" };

    public async Task<SocialPublishResult> PublishAsync(SocialPublishRequest input, CancellationToken cancellationToken)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.AccessToken) ||
            !ProviderInput.IsNumericId(options.PhoneNumberId) || !ProviderInput.IsGraphVersion(options.GraphVersion))
            return Result(BrokerOutcome.Rejected, "not_enabled", "WhatsApp requires explicit enablement, credentials, registered phone, and Graph version.");
        if (!input.RecipientPermissionConfirmed)
            return Result(BrokerOutcome.Rejected, "permission_required", "Confirm recipient permission before WhatsApp messaging.");
        if (!ProviderInput.IsNumericId(input.DestinationId) || input.DestinationId.Length is < 7 or > 15)
            return Result(BrokerOutcome.Rejected, "recipient_required", "WhatsApp requires a recipient number in international digits.");
        if (input.ResumeReceipt is not null)
            return Result(BrokerOutcome.Unsupported, "reconcile_required", "An existing WhatsApp receipt must be reconciled before another send.");
        if (input.Media is { Count: > 1 })
            return Result(BrokerOutcome.Unsupported, "single_photo_required", "This adapter supports one customer-service photo.");

        var payload = new Dictionary<string, object?>
        {
            ["messaging_product"] = "whatsapp", ["recipient_type"] = "individual", ["to"] = input.DestinationId
        };
        if (input.Template is { } template)
        {
            if (input.Media is { Count: > 0 } || string.IsNullOrWhiteSpace(template.Name) ||
                template.Name.Length > 512 || string.IsNullOrWhiteSpace(template.LanguageCode) || template.LanguageCode.Length > 20)
                return Result(BrokerOutcome.Unsupported, "template_format_unsupported", "Only an approved template without parameters/media is supported.");
            payload["type"] = "template";
            payload["template"] = new { name = template.Name, language = new { code = template.LanguageCode } };
        }
        else
        {
            if (!input.CustomerServiceWindowConfirmed)
                return Result(BrokerOutcome.Rejected, "template_required", "Confirm a current customer-service window or select an approved template.");
            var photo = input.Media?.FirstOrDefault();
            if (photo is null)
            {
                if (string.IsNullOrWhiteSpace(input.Text) || input.Text.Length > 4096)
                    return Result(BrokerOutcome.Rejected, "invalid_text", "WhatsApp text must contain at most 4096 characters.");
                payload["type"] = "text";
                payload["text"] = new { body = input.Text };
            }
            else
            {
                if (photo.ContentType is not ("image/png" or "image/jpeg") || !ProviderInput.IsPublicHttps(photo.PublicUrl) ||
                    input.Text.Length > 1024)
                    return Result(BrokerOutcome.Unsupported, "public_photo_required", "Customer-service photo requires public HTTPS PNG/JPEG and a brief caption.");
                payload["type"] = "image";
                payload["image"] = new { link = photo.PublicUrl, caption = input.Text };
            }
        }

        using var request = ProviderHttpTransport.Create(HttpMethod.Post,
            "https://graph.facebook.com/" + options.GraphVersion + "/" + options.PhoneNumberId + "/messages", options.AccessToken);
        request.Content = JsonContent.Create(payload);
        var reply = await ProviderHttpTransport.SendAsync(client, request, true, cancellationToken);
        if (reply.Outcome != BrokerOutcome.Succeeded) return Result(reply.Outcome, reply.Code, reply.Message);
        if (reply.Json is not { } root || root.ValueKind != System.Text.Json.JsonValueKind.Object || !root.TryGetProperty("messages", out var messages) ||
            messages.ValueKind != System.Text.Json.JsonValueKind.Array || messages.GetArrayLength() == 0 ||
            ProviderHttpTransport.Id(messages[0], "id") is not { } id)
            return Result(BrokerOutcome.Ambiguous, "missing_receipt", "WhatsApp accepted the request without a usable message ID.");
        return new(BrokerOutcome.Succeeded, Provider, "accepted", "WhatsApp message accepted; delivery remains unverified.",
            new(Provider, "accepted", id));
    }

    private SocialPublishResult Result(BrokerOutcome outcome, string code, string message)
        => new(outcome, Provider, code, message);
}
