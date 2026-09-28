using System.Text.Json;

namespace OcharoMarketing.Infrastructure.Providers;

/// <summary>Reads bounded identity and error metadata without forwarding provider diagnostics.</summary>
internal static class ProviderReadEvidence
{
    internal static bool HasErrors(JsonElement root)
        => root.ValueKind == JsonValueKind.Object && root.TryGetProperty("errors", out var errors)
            && (errors.ValueKind != JsonValueKind.Array || errors.GetArrayLength() > 0);

    internal static bool IsTrue(JsonElement root, string property)
        => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.True;

    internal static bool TryObject(JsonElement root, string property, out JsonElement value)
    {
        value = default;
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty(property, out value)
            && value.ValueKind == JsonValueKind.Object;
    }

    internal static bool TryTelegramResult(JsonElement? json, out JsonElement result)
    {
        result = default;
        return json is { } root && IsTrue(root, "ok") && TryObject(root, "result", out result);
    }

    internal static string DisplayName(string? value, string fallback)
        => !string.IsNullOrWhiteSpace(value) && value.Length <= 160 && !value.Any(char.IsControl)
            ? value : fallback;
}
