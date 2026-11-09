using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Persistence.Blog;

namespace OcharoBlog.Tests.E2E;

/// <summary>A refused request answers a problem with the status, <c>code</c> and detail Editorial has always answered
/// it with; the Studio and the public pages read them.</summary>
public sealed class BlogProblemContractTests
{
    private const string Recipe = """
        {"crop":null,"rotate":0,"flipHorizontal":false,"flipVertical":false,"resize":null,
         "output":{"format":"png","quality":90},"removeBackground":false}
        """;

    public static TheoryData<string, string, string?, int, string, string> Refusals => new()
    {
        { "POST", "/api/blog/articles", """{"slug":"refused","topic":"style","title":"T","publication":"nope"}""",
            400, "Validation", "Publication must be blog, atlas or wear." },
        { "POST", "/api/blog/articles", """{"slug":"the-nimcha-in-a-new-light","topic":"style","title":"Taken"}""",
            409, "Conflict", "Another story already uses the address 'the-nimcha-in-a-new-light'." },
        { "GET", "/api/blog/articles/{missing}", null, 404, "NotFound", "Story was not found." },
        { "GET", "/preview/{missing}", null, 404, "NotFound", "Story was not found." },
        { "POST", "/api/blog/articles/{first}/publish", """{"expectedRevision":999}""",
            409, "Conflict", "The story changed (now revision 1). Reload first." },
        { "POST", "/api/blog/articles/{missing}/schedule", """{"expectedRevision":1}""",
            400, "Validation", "Choose a publication time." },
        { "POST", "/api/blog/articles/{first}/revisions/99/restore", """{"expectedRevision":1}""",
            404, "NotFound", "Revision was not found." },

        {
            "DELETE",
            "/api/blog/topics/{topic}",
            null,
            409,
            "Conflict",
            "Move or archive the stories in this topic first."
        },
        { "PUT", "/api/blog/topics/Bad%20Key", """{"key":"x","name":"N","short":"S","description":"","sortOrder":0}""",
            400, "Validation", "Topic key must be a short lowercase slug." },
        { "POST", "/api/blog/media/{unknown}/edits/preview", Recipe, 404, "NotFound", "Image was not found." },
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public async Task Refusal_ShouldReturnItsStatusCodeAndDetail_WhenTheBlogRefusesTheRequest(
        string method,
        string path,
        string? body,
        int status,
        string code,
        string detail)
    {
        await using var harness = new BlogHarness();
        using var client = harness.Client();
        var first = (await client.GetFromJsonAsync<JsonElement>("/api/blog/articles")).EnumerateArray().First();
        var target = path
            .Replace("{missing}", Guid.NewGuid().ToString(), StringComparison.Ordinal)
            .Replace("{first}", first.GetProperty("id").GetString(), StringComparison.Ordinal)
            .Replace("{topic}", first.GetProperty("topic").GetString(), StringComparison.Ordinal)
            .Replace("{unknown}", new string('a', 64), StringComparison.Ordinal);
        using var request = new HttpRequestMessage(new HttpMethod(method), target);
        if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request);

        await ShouldRefuseAsync(response, status, code, detail);
    }

    [Fact]
    public async Task Upload_ShouldReturn413_WhenTheImageIsEmpty()
    {
        await using var harness = new BlogHarness(seed: false);
        using var client = harness.Client();
        using var form = new MultipartFormDataContent { { new ByteArrayContent([]), "file", "empty.png" } };

        using var response = await client.PostAsync("/api/blog/media", form);

        await ShouldRefuseAsync(response, 413, "PayloadTooLarge", "Images must contain 1 byte to 20 MiB.");
    }

    [Fact]
    public async Task EditPreview_ShouldReturn503_WhenBackgroundRemovalIsUnavailable()
    {
        await using var harness = new BlogHarness(seed: false);
        using var client = harness.Client();
        using var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(BlogHarness.Png(77)), "file", "a.png" }
        };
        var stored = await (await client.PostAsync("/api/blog/media", form)).Content.ReadFromJsonAsync<JsonElement>();
        var recipe =
            Recipe.Replace("\"removeBackground\":false", "\"removeBackground\":true", StringComparison.Ordinal);
        using var content = new StringContent(recipe, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(
            $"/api/blog/media/{stored.GetProperty("sha256").GetString()}/edits/preview",
            content);

        await ShouldRefuseAsync(
            response,
            503,
            "Unexpected",
            "Background removal is unavailable. Your original image is unchanged.");
    }

    [Fact]
    public async Task Create_ShouldReturn409_WhenAnotherSaveWinsTheRace()
    {
        await using var harness = new BlogHarness(services: services =>
        {
            services.AddScoped<BlogRepository>();
            services.Replace(ServiceDescriptor.Scoped<IBlogRepository>(
                provider => RacingBlogRepository.Over(provider.GetRequiredService<BlogRepository>())));
        });
        using var client = harness.Client();

        using var response = await client.PostAsJsonAsync(
            "/api/blog/articles",
            new { slug = RacingBlogRepository.Slug, topic = "style", title = "Raced" });

        await ShouldRefuseAsync(response, 409, "Conflict", "Another save is finishing. Reload and retry.");
    }

    private static async Task ShouldRefuseAsync(HttpResponseMessage response, int status, string code, string detail)
    {
        ((int)response.StatusCode).Should().Be(status);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(status);
        problem.GetProperty("code").GetString().Should().Be(code);
        problem.GetProperty("detail").GetString().Should().Be(detail);
    }
}
