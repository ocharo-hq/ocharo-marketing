using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using AwesomeAssertions;
using OcharoMarketing.Api.RuntimeConfig.Settings;
using OcharoMarketing.Tests.Support;
using Xunit;

namespace OcharoMarketing.Tests.E2E.RuntimeConfig;

/// <summary>Marketing serves the Ocharo app addresses as public runtime settings, anonymously and uncached.</summary>
[Collection(CapturedLogsCollection.Name)]
public sealed class RuntimeConfigTests
{
    private static readonly Dictionary<string, string?> EveryApp = new()
    {
        ["OcharoApps:Desk"] = "https://ocharo-desk-dev.example.ts.net/",
        ["OcharoApps:Catalogue"] = "https://ocharo-catalogue-dev.example.ts.net/catalogues",
        ["OcharoApps:Studio"] = "https://ocharo-studio-dev.example.ts.net/studio",
        ["OcharoApps:Marketing"] = "https://ocharo-marketing-dev.example.ts.net/",
        ["OcharoApps:Editorial"] = "https://ocharo-blog-dev.example.ts.net/studio/",
        ["OcharoApps:Brand"] = "https://ocharo-brand-dev.example.ts.net/",
        ["OcharoApps:Events"] = "https://ocharo-events-dev.example.ts.net/",
        ["OcharoApps:Atlas"] = "https://ocharo-atlas-dev.example.ts.net/",
        ["OcharoApps:Wear"] = "https://ocharo-wear-dev.example.ts.net/",
        ["OcharoApps:Blog"] = "https://ocharo-blog-dev.example.ts.net/",
    };

    [Fact]
    public async Task RuntimeConfig_ShouldServeEveryConfiguredAddressAnonymouslyAndUncached()
    {
        await using var host = await MarketingHost.CreateAsync(EveryApp);
        using var client = host.Client();

        using var response = await client.GetAsync("/api/runtime-config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        (await ReadAsync(response)).Should().Equal(new Dictionary<string, string>
        {
            ["OCHARO_DESK_URL"] = "https://ocharo-desk-dev.example.ts.net/",
            ["OCHARO_CATALOGUE_URL"] = "https://ocharo-catalogue-dev.example.ts.net/catalogues",
            ["OCHARO_STUDIO_URL"] = "https://ocharo-studio-dev.example.ts.net/studio",
            ["OCHARO_MARKETING_URL"] = "https://ocharo-marketing-dev.example.ts.net/",
            ["OCHARO_EDITORIAL_URL"] = "https://ocharo-blog-dev.example.ts.net/studio/",
            ["OCHARO_BRAND_URL"] = "https://ocharo-brand-dev.example.ts.net/",
            ["OCHARO_EVENTS_URL"] = "https://ocharo-events-dev.example.ts.net/",
            ["OCHARO_ATLAS_URL"] = "https://ocharo-atlas-dev.example.ts.net/",
            ["OCHARO_WEAR_URL"] = "https://ocharo-wear-dev.example.ts.net/",
            ["OCHARO_BLOG_URL"] = "https://ocharo-blog-dev.example.ts.net/",
        });
    }

    [Theory]
    [InlineData("/api/runtime-config")]
    [InlineData("/API/RUNTIME-CONFIG/")]
    [InlineData("/Api/Runtime-Config")]
    public async Task RuntimeConfig_ShouldReturn200WhileTheApiReturns401_WhenThereIsNoSession(string path)
    {
        await using var host = await MarketingHost.CreateAsync(EveryApp);
        using var client = host.Client();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", $"{MarketingTestIdentity.CookieName}=forged");

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        ((await ReadAsync(response)).Keys).Should().Contain("OCHARO_DESK_URL");
        // Sign-in still guards the private API.
        ((await client.GetAsync("/api/marketing/workspace")).StatusCode).Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/health/")]
    [InlineData("/API/SYSTEM/STATUS/")]
    [InlineData("/api/runtime-config/")]
    public async Task TechnicalRoutes_ShouldAnswerEveryVariantWithoutASession(string path)
    {
        await using var host = await MarketingHost.CreateAsync(EveryApp);
        using var client = host.Client();

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RuntimeConfig_ShouldStayClosedToWrites()
    {
        await using var host = await MarketingHost.CreateAsync(EveryApp);
        using var client = host.Client();

        using var response = await client.PostAsync("/api/runtime-config", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RuntimeConfig_ShouldDropAddressesAndWarnOnce_WhenTheyAreNotPlainHttpsLinks()
    {
        var logs = new CapturedLogs();
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>
        {
            ["OcharoApps:Desk"] = "http://ocharo-desk-dev.example.ts.net/",
            ["OcharoApps:Catalogue"] = "https://operator:secret@ocharo-catalogue-dev.example.ts.net/",
            ["OcharoApps:Studio"] = "https://ocharo-studio-dev.example.ts.net/studio?token=abc",
            ["OcharoApps:Marketing"] = "https://ocharo-marketing-dev.example.ts.net/#access_token=abc",
            ["OcharoApps:Editorial"] = "/studio/",
            ["OcharoApps:Brand"] = "not a link",
            ["OcharoApps:Events"] = "http://ocharo-events-dev.example.ts.net/",
            ["OcharoApps:Atlas"] = "https://ocharo-atlas-dev.example.ts.net/",
            ["OcharoApps:Wear"] = "",
            ["OcharoApps:Blog"] = "javascript:alert(1)",
        }, logs: logs);
        using var client = host.Client();

        using var response = await client.GetAsync("/api/runtime-config");
        await client.GetAsync("/api/runtime-config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync(response))
            .Should()
            .Equal(
                new Dictionary<string, string> { ["OCHARO_ATLAS_URL"] = "https://ocharo-atlas-dev.example.ts.net/" });
        var warning = logs.Warnings
            .Should()
            .ContainSingle(message => message.StartsWith("Runtime config leaves out", StringComparison.Ordinal)).Which;
        warning
            .Should()
            .Contain(
                "OcharoApps:Desk, OcharoApps:Catalogue, OcharoApps:Studio, OcharoApps:Marketing, "
                    + "OcharoApps:Editorial, OcharoApps:Brand, OcharoApps:Events, OcharoApps:Blog");
        warning.Should().NotContain("secret");
    }

    [Fact]
    public async Task RuntimeConfig_ShouldServeAnEmptyObjectWithoutAWarning_WhenNoAddressIsConfigured()
    {
        var logs = new CapturedLogs();
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>(), logs: logs);
        using var client = host.Client();

        using var response = await client.GetAsync("/api/runtime-config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        (await response.Content.ReadAsStringAsync()).Should().Be("{}");
        logs.Warnings.Should().NotContain(message => message.StartsWith("Runtime config", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("https://ocharo-desk-dev.example.ts.net/", true)]
    [InlineData("https://desk.example.com:8443/organizations", true)]
    [InlineData("https://example.com/@handle", true)]
    [InlineData("http://desk.example.com/", false)]
    [InlineData("https://user@desk.example.com/", false)]
    [InlineData("https://desk.example.com/?", false)]
    [InlineData("https://desk.example.com/#", false)]
    [InlineData("https://desk.example.com/a b", false)]
    [InlineData(" https://desk.example.com/", false)]
    [InlineData("//desk.example.com/", false)]
    [InlineData("/organizations", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsPublicAddress_ShouldAcceptOnlyPlainAbsoluteHttpsLinks(string? value, bool accepted)
    {
        OcharoAppsSettingsExtensions.IsPublicAddress(value).Should().Be(accepted);
    }

    [Fact]
    public void RejectedSettings_ShouldNameConfiguredAddressesItLeavesOut()
    {
        var settings = new OcharoAppsSettings
        {
            Desk = "http://desk.example.com/",
            Atlas = "https://atlas.example.com/",
            Wear = " "
        };

        settings.RejectedSettings().Should().Equal(["OcharoApps:Desk"]);
    }

    private static async Task<Dictionary<string, string>> ReadAsync(HttpResponseMessage response)
    {
        return JsonSerializer.Deserialize<Dictionary<string, string>>(await response.Content.ReadAsStringAsync())!;
    }
}
