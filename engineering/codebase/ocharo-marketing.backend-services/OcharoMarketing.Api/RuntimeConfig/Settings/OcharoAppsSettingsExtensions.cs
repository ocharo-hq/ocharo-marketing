using OcharoMarketing.Api.RuntimeConfig.Models;

namespace OcharoMarketing.Api.RuntimeConfig.Settings;

/// <summary>Projects the Ocharo app addresses onto the runtime-config keys the Marketing SPA declares.</summary>
public static class OcharoAppsSettingsExtensions
{
    /// <summary>Pairs each setting with its reader, in the app menu's order.</summary>
    private static readonly (string Setting, Func<OcharoAppsSettings, string?> Read)[] Addresses =
    [
        (nameof(OcharoAppsSettings.Desk), settings => settings.Desk),
        (nameof(OcharoAppsSettings.Catalogue), settings => settings.Catalogue),
        (nameof(OcharoAppsSettings.Studio), settings => settings.Studio),
        (nameof(OcharoAppsSettings.Marketing), settings => settings.Marketing),
        (nameof(OcharoAppsSettings.Editorial), settings => settings.Editorial),
        (nameof(OcharoAppsSettings.Brand), settings => settings.Brand),
        (nameof(OcharoAppsSettings.Atlas), settings => settings.Atlas),
        (nameof(OcharoAppsSettings.Wear), settings => settings.Wear),
        (nameof(OcharoAppsSettings.Blog), settings => settings.Blog),
    ];

    /// <summary>Returns the configured, plain HTTPS addresses for runtime config; any other value is left
    /// out.</summary>
    public static OcharoRuntimeConfig ToRuntimeConfig(this OcharoAppsSettings settings) => new()
    {
        DeskUrl = Public(settings.Desk),
        CatalogueUrl = Public(settings.Catalogue),
        StudioUrl = Public(settings.Studio),
        MarketingUrl = Public(settings.Marketing),
        EditorialUrl = Public(settings.Editorial),
        BrandUrl = Public(settings.Brand),
        AtlasUrl = Public(settings.Atlas),
        WearUrl = Public(settings.Wear),
        BlogUrl = Public(settings.Blog),
    };

    /// <summary>Names the settings that are configured but not plain HTTPS addresses, so the host can warn about them.</summary>
    public static IReadOnlyList<string> RejectedSettings(this OcharoAppsSettings settings)
    {
        return Addresses
            .Where(address => !string.IsNullOrWhiteSpace(address.Read(settings)) && !IsPublicAddress(address.Read(settings)))
            .Select(address => $"{OcharoAppsSettings.SectionName}:{address.Setting}")
            .ToArray();
    }

    /// <summary>Accepts an absolute HTTPS address without credentials, query or fragment that could carry a token.</summary>
    public static bool IsPublicAddress(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.Host.Length > 0
            && uri.UserInfo.Length == 0
            && value.IndexOfAny(['?', '#']) < 0
            && !value.Any(char.IsWhiteSpace);
    }

    /// <summary>Keeps <paramref name="value"/> when it is a public address, else nothing.</summary>
    private static string? Public(string? value) => IsPublicAddress(value) ? value : null;
}
