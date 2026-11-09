using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OcharoBlog.Api.Blog.Controllers;
using OcharoBlog.Infrastructure.Blog;
using Xunit;

namespace OcharoBlog.Tests.E2E;

public sealed class InquiryRelayTests
{
    [Fact]
    public async Task Relay_ShouldKeepTheSubmissionIdentity_WhenTheReceiverTimesOutAfterReceipt()
    {
        var handler = new ReceiptHandler();
        var controller = Controller(handler);
        var input = JsonSerializer.SerializeToElement(new
        {
            submissionId = Guid.NewGuid().ToString(), name = "Synthetic buyer", contactMethod = "email",
            contact = "buyer@example.test", message = "Synthetic availability enquiry.",
            sourcePath = "/catalogue/pieces/abaya/", productId = "abaya-v01", consent = true
        });
        var first = (await controller.Submit(input, CancellationToken.None)).Should().BeOfType<ObjectResult>().Subject;
        first.StatusCode.Should().Be(503);
        var problem = first.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Contain("may already be saved");
        problem.Detail.Should().NotContain("has not been submitted");
        var retry = (await controller.Submit(input, CancellationToken.None)).Should().BeOfType<ContentResult>().Subject;
        retry.StatusCode.Should().Be(202);
        retry.Content.Should().Contain("synthetic-reference");
        handler.Attempts.Count.Should().Be(2);
        handler.Attempts[1].Should().Be(handler.Attempts[0]);
        handler.Stored.Should().ContainSingle();
        controller.Response.Headers.CacheControl.ToString().Should().Be("no-store");
    }

    [Fact]
    public async Task Relay_ShouldPropagateCancellation_WhenTheCallerCancels()
    {
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var controller = Controller(new CancelledHandler());
        await FluentActions.Awaiting(() =>
            controller.Submit(
                JsonSerializer.SerializeToElement(new { submissionId = "test" }),
                cancelled.Token)).Should()
                .ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://localhost:8355")]
    [InlineData("https://user:password@localhost:8354")]
    public async Task Relay_ShouldReturn503WithoutSending_WhenTheReceiverIsMissingOrUnsafe(string? origin)
    {
        var handler = new ReceiptHandler();
        var controller = Controller(handler, origin);
        var response = (await controller.Submit(
            JsonSerializer.SerializeToElement(new { submissionId = "test" }), CancellationToken.None)).Should()
                .BeOfType<ObjectResult>()
                .Subject;
        response.StatusCode.Should().Be(503);
        handler.Attempts.Should().BeEmpty();
        controller.Response.Headers.CacheControl.ToString().Should().Be("no-store");
    }

    [Fact]
    public async Task Relay_ShouldKeepValidationAndHeadersButNoCookies_WhenTheReceiverRefuses()
    {
        var response = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content =
                JsonContent.Create(new { type =
                    "https://example.test/problems/validation", title = "Check the enquiry", status = 422,
                detail =
                    "Enter a contact address.", traceId =
                        "marketing-trace", requestId = "marketing-request", code = "Validation",
                errors = new Dictionary<string, string[]>
                {
                    ["contact"] = ["Contact is required."]
                }, secret = "private-token" })
        };
        response.Content.Headers.ContentType = new("application/problem+json");
        response.Headers.RetryAfter = new(System.TimeSpan.FromSeconds(12));
        response.Headers.WwwAuthenticate.ParseAdd("Bearer realm=\"enquiries\"");
        response.Content.Headers.Allow.Add("POST");
        response.Headers.TryAddWithoutValidation("Set-Cookie", "secret=private-token");
        var controller = Controller(new StaticHandler(response));
        var result =
            (await controller.Submit(JsonSerializer.SerializeToElement(new { }), default))
                .Should()
                .BeOfType<ObjectResult>()
                .Subject;
        var problem = result.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        result.StatusCode.Should().Be(422);
        problem.Detail.Should().Be("Enter a contact address.");
        problem.Errors["contact"].Should().Equal(new[] { "Contact is required." });
        problem.Extensions["upstreamTraceId"].Should().Be("marketing-trace");
        problem.Extensions["upstreamRequestId"].Should().Be("marketing-request");
        problem.Extensions["code"].Should().Be("Validation");
        controller.Response.Headers.RetryAfter.ToString().Should().Be("12");
        controller.Response.Headers.WWWAuthenticate.ToString().Should().Be("Bearer realm=\"enquiries\"");
        controller.Response.Headers.Allow.ToString().Should().Be("POST");
        controller.Response.Headers.ContainsKey("Set-Cookie").Should().BeFalse();
        problem.Extensions.ContainsKey("secret").Should().BeFalse();
    }

    [Fact]
    public async Task Relay_ShouldStripInternalDetails_WhenTheReceiverFails()
    {
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content =
                JsonContent.Create(new { type =
                    "https://private.internal/fail", title = "private-database", status = 503,
                detail = "private-password SQL stacktrace", traceId = "safe-trace", requestId = "safe-request",
                errors = new Dictionary<string, string[]>
                {
                    ["sql"] = ["private-password"]
                }, stack = "private-stack", code = "private-secret" })
        };
        response.Content.Headers.ContentType = new("application/problem+json");
        response.Headers.RetryAfter = new(System.TimeSpan.FromSeconds(30));
        var controller = Controller(new StaticHandler(response));
        var result =
            (await controller.Submit(JsonSerializer.SerializeToElement(new { }), default))
                .Should()
                .BeOfType<ObjectResult>()
                .Subject;
        var problem = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(503);
        problem.Type.Should().Be("about:blank");
        problem.Title.Should().Be("Service Unavailable");
        problem.Detail.Should().Contain("may already be saved");
        JsonSerializer.Serialize(problem).Should().NotContain("private");
        problem.Extensions["upstreamTraceId"].Should().Be("safe-trace");
        controller.Response.Headers.RetryAfter.ToString().Should().Be("30");
    }

    [Theory]
    [InlineData("text/html", "<html>private-stack</html>")]
    [InlineData("application/problem+json", "{broken")]
    [InlineData("application/problem+json", "{\"type\":\"about:blank\",\"title\":\"private-detail\",\"status\":200}")]
    [InlineData(
        "application/problem+json",
        "{\"type\":\"about:blank\",\"title\":\"private-detail\",\"status\":\"400\"}")]
    public async Task Relay_ShouldAnswerASafeProblem_WhenTheReceiverErrorIsNonstandard(string contentType, string body)
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body) };
        response.Content.Headers.ContentType = new(contentType);
        var controller = Controller(new StaticHandler(response));
        var result =
            (await controller.Submit(JsonSerializer.SerializeToElement(new { }), default))
                .Should()
                .BeOfType<ObjectResult>()
                .Subject;
        var problem = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(400);
        JsonSerializer.Serialize(problem).Should().NotContain("private");
    }

    [Fact]
    public async Task Relay_ShouldKeepTheBodyContract_WhenTheReceiverAccepts()
    {
        const string body = "{\"reference\":\"receipt-42\",\"duplicate\":true}";
        var controller =
            Controller(new StaticHandler(new(HttpStatusCode.Accepted) { Content = new StringContent(body) }));
        var result =
            (await controller.Submit(JsonSerializer.SerializeToElement(new { }), default))
                .Should()
                .BeOfType<ContentResult>()
                .Subject;
        result.StatusCode.Should().Be(202);
        result.ContentType.Should().Be("application/json");
        result.Content.Should().Be(body);
    }

    [Fact]
    public async Task Relay_ShouldKeepTheErrorWireContract_WhenServedThroughTheHost()
    {
        var upstream = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = JsonContent.Create(new { type = "about:blank", title = "Validation", status = 422,
                detail = "Enter a contact address.", traceId = "marketing-trace", requestId = "marketing-request",
                errors = new Dictionary<string, string[]> { ["contact"] = ["Contact is required."] } })
        };
        upstream.Content.Headers.ContentType = new("application/problem+json");
        upstream.Headers.RetryAfter = new(System.TimeSpan.FromSeconds(12));
        await using var harness = new BlogHarness(seed: false, services: collection =>
            collection.AddHttpClient("ocharo-inquiries")
                .ConfigurePrimaryHttpMessageHandler(() => new StaticHandler(upstream)));
        await using var factory =
            harness.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Blog:MarketingBaseUrl"] = "https://receiver.test"
                })));
        using var client =
            factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        using var response = await client.PostAsJsonAsync("/api/public/inquiries", new { submissionId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (response.Content.Headers.ContentType?.MediaType).Should().Be("application/problem+json");
        (response.Headers.RetryAfter?.Delta).Should().Be(TimeSpan.FromSeconds(12));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("contact")[0].GetString().Should().Be("Contact is required.");
        body.GetProperty("upstreamTraceId").GetString().Should().Be("marketing-trace");
        body.GetProperty("upstreamRequestId").GetString().Should().Be("marketing-request");
        body.GetProperty("traceId").GetString().Should().NotBe("marketing-trace");
        body.GetProperty("instance").GetString().Should().Be("/api/public/inquiries");
    }

    [Theory]
    [InlineData(false, "/api/public/inquiries")]
    [InlineData(true, "/api/public/inquiries")]
    [InlineData(true, "/API/PUBLIC/INQUIRIES")]
    public async Task Relay_ShouldAcceptAnEnquiry_WhenAnonymousOrSignedInWithoutACsrfToken(bool session, string path)
    {
        const string body = "{\"reference\":\"receipt-42\"}";
        await using var host =
            RelayHost(new StaticHandler(new(HttpStatusCode.Accepted) { Content = new StringContent(body) }));
        using var client = session ? EditorialSignIn.Client(host.Factory, EditorialSignIn.Owner, csrf: false)
            : EditorialSignIn.Anonymous(host.Factory);

        using var response = await client.PostAsJsonAsync(path, new { submissionId = "test" });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadAsStringAsync()).Should().Be(body);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://evil.example", "/api/public/inquiries")]
    [InlineData("https://evil.example", "/API/PUBLIC/INQUIRIES")]
    [InlineData("http://localhost", "/api/public/inquiries")]
    [InlineData("null", "/api/public/inquiries")]
    public async Task Relay_ShouldReturn403_WhenTheEnquiryIsCrossOrigin(string origin, string path)
    {
        var handler = new ReceiptHandler();
        await using var host = RelayHost(handler);
        using var client = EditorialSignIn.Anonymous(host.Factory);
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
            { Content = JsonContent.Create(new { submissionId = "test" }) };
        request.Headers.Add("Origin", origin);

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        handler.Attempts.Should().BeEmpty();
    }

    // https://localhost:8284 is the local Vite origin, which a local run trusts through appsettings.Local.json.
    [Theory]
    [InlineData("https://localhost")]
    [InlineData("https://localhost:8284")]
    public async Task Relay_ShouldAcceptAnEnquiry_WhenItComesFromThisOrATrustedOrigin(string origin)
    {
        await using var host =
            RelayHost(new StaticHandler(new(HttpStatusCode.Accepted) { Content = new StringContent("{}") }));
        using var client = EditorialSignIn.Anonymous(host.Factory);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/public/inquiries")
            { Content = JsonContent.Create(new { submissionId = "test" }) };
        request.Headers.Add("Origin", origin);

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    private static EditorialHost RelayHost(HttpMessageHandler upstream) => EditorialHost.Create(
        new Dictionary<string, string?>
        {
            ["Blog:MarketingBaseUrl"] = "https://receiver.test", ["Blog:TrustedOrigins:0"] = "https://localhost:8284",
        },
        services: collection =>
            collection.AddHttpClient("ocharo-inquiries").ConfigurePrimaryHttpMessageHandler(() => upstream));

    private sealed class StaticHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            return Task.FromResult(response);
        }
    }

    private static PublicInquiriesController Controller(HttpMessageHandler handler,
        string? origin = "https://localhost:8354")
    {
        return new(new BlogOptions { MarketingBaseUrl = origin }, new Factory(handler))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext

            {
                RequestServices = new ServiceCollection().AddLogging().AddControllers().Services.BuildServiceProvider()
            } }
        };
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class ReceiptHandler : HttpMessageHandler
    {
        public List<string> Attempts { get; } = [];
        public Dictionary<string, string> Stored { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = await request.Content!.ReadAsStringAsync(ct); Attempts.Add(body);
            using var input = JsonDocument.Parse(body);
            var key = input.RootElement.GetProperty("submissionId").GetString()!;
            Stored.TryAdd(key, body);
            if (Attempts.Count == 1) throw new TaskCanceledException("Simulated lost response after persistence.");
            return new(HttpStatusCode.Accepted)
            {
                Content = JsonContent.Create(new { reference = "synthetic-reference" })
            };
        }
    }

    private sealed class CancelledHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromCanceled<HttpResponseMessage>(ct);
    }
}
