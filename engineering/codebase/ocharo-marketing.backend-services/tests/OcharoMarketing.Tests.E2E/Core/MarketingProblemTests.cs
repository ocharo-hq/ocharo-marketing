using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using AwesomeAssertions;
using OcharoMarketing.Api.Core.Errors;
using WoW.Two.Sdk.Beta.Be.Web.ErrorMapping;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Factories;
using Xunit;
using static OcharoMarketing.Tests.Support.MarketingTestIdentity;

namespace OcharoMarketing.Tests.E2E.Core;

/// <summary>A refused command answers its status as a problem through the be sdk's problem factory, with the
/// <c>code</c>, title and detail the desk read before the mediator, uncached.</summary>
public sealed class MarketingProblemTests
{
    [Fact]
    public async Task Generate_ShouldReturn409RejectedProblem_WhenTheProviderIsDisabled()
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = await SignedIn(host.Factory, Owner);

        using var response = await client.PostAsJsonAsync(
            "/api/marketing/generation-jobs",
            new { provider = "openai", prompt = "A linen dress" });

        var problem = await ProblemAsync(response, HttpStatusCode.Conflict);
        (problem.GetProperty("code").GetString()).Should().Be("Conflict");
        (problem.GetProperty("title").GetString()).Should().Be("Marketing command rejected");
        (problem.GetProperty("detail").GetString()).Should().Be("This image provider is not enabled and configured.");
        (problem.GetProperty("instance").GetString()).Should().Be("/api/marketing/generation-jobs");
        (string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString())).Should().BeFalse();
        var services = host.Factory.Services;
        (services.GetRequiredService<IErrorHttpStatusCodeMapper>())
            .Should()
            .BeOfType<MarketingErrorHttpStatusCodeMapper>();
        (services.GetRequiredService<IAppErrorProblemDetailsFactory>())
            .Should()
            .BeOfType<MarketingProblemDetailsFactory>();
    }

    [Fact]
    public async Task Upload_ShouldReturn413PayloadTooLarge_WhenTheFileIsEmpty()
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = await SignedIn(host.Factory, Owner);
        using var form = new MultipartFormDataContent
        {
            { new ByteArrayContent([]), "file", "empty.png" },
            { new StringContent("Test"), "provenance" },
        };

        using var response = await client.PostAsync("/api/marketing/assets", form);

        var problem = await ProblemAsync(response, HttpStatusCode.RequestEntityTooLarge);
        (problem.GetProperty("code").GetString()).Should().Be("PayloadTooLarge");
        (problem.GetProperty("type").GetString()).Should().Be("urn:wow-two:error:PayloadTooLarge");
        (problem.GetProperty("detail").GetString()).Should().Be("Choose a nonempty image smaller than 25 MB.");
    }

    [Fact]
    public async Task Upload_ShouldReturn415UnsupportedMediaType_WhenTheFileIsNotAnImage()
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = await SignedIn(host.Factory, Owner);
        using var form = new MultipartFormDataContent
        {
            { new ByteArrayContent([1, 2, 3]), "file", "notes.png" },
            { new StringContent("Test"), "provenance" },
        };

        using var response = await client.PostAsync("/api/marketing/assets", form);

        var problem = await ProblemAsync(response, HttpStatusCode.UnsupportedMediaType);
        (problem.GetProperty("code").GetString()).Should().Be("UnsupportedMediaType");
        (problem.GetProperty("type").GetString()).Should().Be("urn:wow-two:error:UnsupportedMediaType");
        (problem.GetProperty("title").GetString()).Should().Be("Marketing command rejected");
    }

    [Fact]
    public async Task Approve_ShouldReturn404NotFound_WhenTheDraftIsUnknown()
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = await SignedIn(host.Factory, Owner);

        using var response = await client.PostAsJsonAsync(
            "/api/marketing/drafts/missing/approve",
            new { expectedRevision = 1 });

        var problem = await ProblemAsync(response, HttpStatusCode.NotFound);
        (problem.GetProperty("code").GetString()).Should().Be("NotFound");
        (problem.GetProperty("detail").GetString()).Should().Be("Draft not found.");
    }

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        response.StatusCode.Should().Be(expected);
        (response.Content.Headers.ContentType?.MediaType).Should().Be("application/problem+json");
        (response.Headers.CacheControl?.NoStore).Should().BeTrue();
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        (problem.GetProperty("status").GetInt32()).Should().Be((int)expected);
        return problem;
    }
}
