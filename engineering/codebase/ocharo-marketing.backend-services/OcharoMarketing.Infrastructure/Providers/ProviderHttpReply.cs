using System.Text.Json;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers;

/// <summary>Carries parsed provider evidence without exposing error bodies or credentials.</summary>
internal sealed record ProviderHttpReply(
    BrokerOutcome Outcome,
    JsonElement? Json,
    string Code,
    string Message,
    string? RequestId = null);
