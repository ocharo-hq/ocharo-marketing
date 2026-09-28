using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers;

/// <summary>Validates bounded provider inputs before external execution.</summary>
internal static partial class ProviderInput
{
    internal static bool PromptIsValid(string? prompt)
        => !string.IsNullOrWhiteSpace(prompt) && prompt.Length <= 20000;

    internal static bool ImagesAreValid(IReadOnlyList<ImageReference>? references)
        => references is null || references.Count <= 16 &&
           references.All(image => image.Data is { Length: > 0 } && IsImage(image.ContentType)) &&
           references.Sum(image => (long)image.Data.Length) <= 26_214_400;

    internal static bool IsImage(string? contentType)
        => contentType is "image/png" or "image/jpeg" or "image/webp";

    internal static bool IsGraphVersion(string? version)
        => version is not null && GraphVersionPattern().IsMatch(version);

    internal static bool IsNumericId(string? id)
        => id is not null && NumericIdPattern().IsMatch(id);

    internal static bool IsTelegramAccountId(string? id)
        => IsTelegramChatId(id) && id![0] != '-';

    internal static bool IsTelegramChatId(string? id)
        => id is not null && long.TryParse(id, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
            && value != 0 && value is >= -4_503_599_627_370_495 and <= 4_503_599_627_370_495
            && value.ToString(CultureInfo.InvariantCulture) == id;

    internal static bool IsTelegramUsername(string? value)
        => value is not null && TelegramUsernamePattern().IsMatch(value);

    internal static bool IsPublicHttps(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            uri.IsLoopback || !string.IsNullOrEmpty(uri.UserInfo)) return false;
        if (!IPAddress.TryParse(uri.Host, out var address)) return !uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase);
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
        var bytes = address.GetAddressBytes();
        return bytes[0] is not (0 or 10 or 127) && !(bytes[0] == 169 && bytes[1] == 254) &&
               !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31) &&
               !(bytes[0] == 192 && bytes[1] == 168);
    }

    internal static bool IsGeneratedImage(byte[] bytes, string contentType)
        => contentType switch
        {
            "image/png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "image/jpeg" => bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255,
            "image/webp" => bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                            bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };

    internal static string AsDataUrl(ImageReference image)
        => "data:" + image.ContentType + ";base64," + Convert.ToBase64String(image.Data);

    [GeneratedRegex(@"^v\d{1,3}\.\d{1,2}$", RegexOptions.CultureInvariant)]
    private static partial Regex GraphVersionPattern();

    [GeneratedRegex(@"^[0-9]{1,40}$", RegexOptions.CultureInvariant)]
    private static partial Regex NumericIdPattern();

    [GeneratedRegex(@"^@[A-Za-z0-9_]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex TelegramUsernamePattern();
}
