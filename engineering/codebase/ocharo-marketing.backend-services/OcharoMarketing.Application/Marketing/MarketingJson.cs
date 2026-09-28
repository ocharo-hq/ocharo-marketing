using System.Text.Json;

namespace OcharoMarketing.Application.Marketing;

/// <summary>Serializes persisted marketing documents with one versioned camel-case shape.</summary>
public static class MarketingJson
{
    /// <summary>Gets the shared document serializer options.</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);

    /// <summary>Serializes the supplied marketing document.</summary>
    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);

    /// <summary>Reads a required marketing document.</summary>
    public static T Read<T>(string value) => JsonSerializer.Deserialize<T>(value, Options)
        ?? throw new InvalidOperationException("The stored marketing document is empty.");
}
