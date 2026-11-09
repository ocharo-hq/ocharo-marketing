using System.Net;

namespace OcharoBlog.Tests.E2E;

/// <summary>A public path no page answers gets the public site's not-found page; the Studio, the API, media and
/// previews keep their own answers, so the public catch-all never outranks them.</summary>
public sealed class PublicNotFoundTests
{
    [Theory]
    [InlineData("/journal/missing/")]
    [InlineData("/nope")]
    [InlineData("/topics/missing/")]
    [InlineData("/journal/a/b")]
    [InlineData("/atlas/missing/")]
    [InlineData("/uz/nope")]
    public async Task PublicMiss_ShouldReturn404WithTheSitesNotFoundPage_WhenNoPageMatches(string path)
    {
        await using var harness = new BlogHarness();
        using var client = harness.Anonymous();

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (response.Content.Headers.ContentType?.MediaType).Should().Be("text/html");
        var html = await response.Content.ReadAsStringAsync();
        html.Should().StartWith("<!doctype html><html lang=\"en\">");
        html.Should().Contain("<header class=\"site-header\">");
        html.Should().Contain("<footer class=\"site-footer\">");
        html.Should().Contain("<meta name=\"robots\" content=\"noindex,nofollow\">");
        html.Should().Contain("<h1>Page not found<span class=\"title-dot\">.</span></h1>");
        html.Should().Contain("<a class=\"filled-link\" href=\"/\">Blog home");
        html.Should().NotContain("rel=\"canonical\"");
    }

    [Fact]
    public async Task PublicMiss_ShouldReturn404_WhenAskedWithHead()
    {
        await using var harness = new BlogHarness();
        using var client = harness.Anonymous();

        using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/nope"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (response.Content.Headers.ContentType?.MediaType).Should().Be("text/html");
    }

    [Fact]
    public async Task Story_ShouldReturn200_WhenItIsPublished()
    {
        await using var harness = new BlogHarness();
        using var client = harness.Anonymous();

        using var response = await client.GetAsync("/journal/the-nimcha-in-a-new-light/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("Page not found");
    }

    [Theory]
    [InlineData("/api/x")]
    [InlineData("/media/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("/preview/nope")]
    public async Task OwnMiss_ShouldReturn404ProblemJson_WhenTheRouteKeepsItsOwnAnswer(string path)
    {
        await using var harness = new BlogHarness();
        using var client = harness.Client();

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (response.Content.Headers.ContentType?.MediaType).Should().Be("application/problem+json");
    }

    [Fact]
    public async Task StudioRoute_ShouldServeTheShell_WhenThePublicCatchAllAlsoMatches()
    {
        await using var harness = new BlogHarness();
        using var client = harness.Client();

        using var shell = await client.GetAsync("/studio/");
        using var route = await client.GetAsync("/studio/x");
        using var bare = await client.GetAsync("/studio");

        route.StatusCode.Should().Be(HttpStatusCode.OK);
        (route.Content.Headers.ContentType?.MediaType).Should().Be("text/html");
        (await route.Content.ReadAsStringAsync()).Should().Be(await shell.Content.ReadAsStringAsync());
        bare.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        (bare.Headers.Location?.OriginalString).Should().EndWith("/studio/");
    }
}
