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
using Xunit;

namespace OcharoBlog.Tests;

public sealed class InquiryRelayTests
{
    [Fact]
    public async Task Timeout_after_receipt_retains_submission_identity_for_safe_retry()
    {
        var handler = new ReceiptHandler();
        var controller = Controller(handler);
        var input = JsonSerializer.SerializeToElement(new
        {
            submissionId = Guid.NewGuid().ToString(), name = "Synthetic buyer", contactMethod = "email",
            contact = "buyer@example.test", message = "Synthetic availability enquiry.",
            sourcePath = "/catalogue/pieces/abaya/", productId = "abaya-v01", consent = true
        });
        var first = Assert.IsType<ObjectResult>(await controller.Submit(input, CancellationToken.None));
        Assert.Equal(503, first.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(first.Value);
        Assert.Contains("may already be saved", problem.Detail);
        Assert.DoesNotContain("has not been submitted", problem.Detail);
        var retry = Assert.IsType<ContentResult>(await controller.Submit(input, CancellationToken.None));
        Assert.Equal(202, retry.StatusCode);
        Assert.Contains("synthetic-reference", retry.Content);
        Assert.Equal(2, handler.Attempts.Count);
        Assert.Equal(handler.Attempts[0], handler.Attempts[1]);
        Assert.Single(handler.Stored);
        Assert.Equal("no-store", controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Caller_cancellation_propagates_without_a_misleading_delivery_response()
    {
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var controller = Controller(new CancelledHandler());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            controller.Submit(JsonSerializer.SerializeToElement(new { submissionId = "test" }), cancelled.Token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://localhost:8355")]
    [InlineData("https://user:password@localhost:8354")]
    public async Task Missing_or_unsafe_receiver_never_sends_buyer_data(string? origin)
    {
        var handler = new ReceiptHandler();
        var controller = Controller(handler, origin);
        var response = Assert.IsType<ObjectResult>(await controller.Submit(
            JsonSerializer.SerializeToElement(new { submissionId = "test" }), CancellationToken.None));
        Assert.Equal(503, response.StatusCode);
        Assert.Empty(handler.Attempts);
        Assert.Equal("no-store", controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Upstream_validation_and_semantic_headers_are_retained_without_relaying_cookies()
    {
        var response = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = JsonContent.Create(new { type = "https://example.test/problems/validation", title = "Check the enquiry", status = 422,
                detail = "Enter a contact address.", traceId = "marketing-trace", requestId = "marketing-request", code = "Validation",
                errors = new Dictionary<string, string[]> { ["contact"] = ["Contact is required."] }, secret = "private-token" })
        };
        response.Content.Headers.ContentType = new("application/problem+json");
        response.Headers.RetryAfter = new(System.TimeSpan.FromSeconds(12));
        response.Headers.WwwAuthenticate.ParseAdd("Bearer realm=\"enquiries\"");
        response.Content.Headers.Allow.Add("POST");
        response.Headers.TryAddWithoutValidation("Set-Cookie", "secret=private-token");
        var controller = Controller(new StaticHandler(response));
        var result = Assert.IsType<ObjectResult>(await controller.Submit(JsonSerializer.SerializeToElement(new { }), default));
        var problem = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Equal(422, result.StatusCode);
        Assert.Equal("Enter a contact address.", problem.Detail);
        Assert.Equal(new[] { "Contact is required." }, problem.Errors["contact"]);
        Assert.Equal("marketing-trace", problem.Extensions["upstreamTraceId"]);
        Assert.Equal("marketing-request", problem.Extensions["upstreamRequestId"]);
        Assert.Equal("Validation", problem.Extensions["code"]);
        Assert.Equal("12", controller.Response.Headers.RetryAfter.ToString());
        Assert.Equal("Bearer realm=\"enquiries\"", controller.Response.Headers.WWWAuthenticate.ToString());
        Assert.Equal("POST", controller.Response.Headers.Allow.ToString());
        Assert.False(controller.Response.Headers.ContainsKey("Set-Cookie"));
        Assert.False(problem.Extensions.ContainsKey("secret"));
    }

    [Fact]
    public async Task Upstream_server_failures_keep_correlation_and_retry_but_strip_internal_details()
    {
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = JsonContent.Create(new { type = "https://private.internal/fail", title = "private-database", status = 503,
                detail = "private-password SQL stacktrace", traceId = "safe-trace", requestId = "safe-request",
                errors = new Dictionary<string, string[]> { ["sql"] = ["private-password"] }, stack = "private-stack", code = "private-secret" })
        };
        response.Content.Headers.ContentType = new("application/problem+json");
        response.Headers.RetryAfter = new(System.TimeSpan.FromSeconds(30));
        var controller = Controller(new StaticHandler(response));
        var result = Assert.IsType<ObjectResult>(await controller.Submit(JsonSerializer.SerializeToElement(new { }), default));
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(503, problem.Status);
        Assert.Equal("about:blank", problem.Type);
        Assert.Equal("Service Unavailable", problem.Title);
        Assert.Contains("may already be saved", problem.Detail);
        Assert.DoesNotContain("private", JsonSerializer.Serialize(problem));
        Assert.Equal("safe-trace", problem.Extensions["upstreamTraceId"]);
        Assert.Equal("30", controller.Response.Headers.RetryAfter.ToString());
    }

    [Theory]
    [InlineData("text/html", "<html>private-stack</html>")]
    [InlineData("application/problem+json", "{broken")]
    [InlineData("application/problem+json", "{\"type\":\"about:blank\",\"title\":\"private-detail\",\"status\":200}")]
    [InlineData("application/problem+json", "{\"type\":\"about:blank\",\"title\":\"private-detail\",\"status\":\"400\"}")]
    public async Task Nonstandard_or_mismatched_upstream_errors_become_safe_problems(string contentType, string body)
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body) };
        response.Content.Headers.ContentType = new(contentType);
        var controller = Controller(new StaticHandler(response));
        var result = Assert.IsType<ObjectResult>(await controller.Submit(JsonSerializer.SerializeToElement(new { }), default));
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(400, problem.Status);
        Assert.DoesNotContain("private", JsonSerializer.Serialize(problem));
    }

    [Fact]
    public async Task Successful_receiver_response_keeps_its_existing_body_contract()
    {
        const string body = "{\"reference\":\"receipt-42\",\"duplicate\":true}";
        var controller = Controller(new StaticHandler(new(HttpStatusCode.Accepted) { Content = new StringContent(body) }));
        var result = Assert.IsType<ContentResult>(await controller.Submit(JsonSerializer.SerializeToElement(new { }), default));
        Assert.Equal(202, result.StatusCode);
        Assert.Equal("application/json", result.ContentType);
        Assert.Equal(body, result.Content);
    }

    [Fact]
    public async Task Relay_errors_keep_the_wire_contract_through_the_full_http_pipeline()
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
            collection.AddHttpClient("ocharo-inquiries").ConfigurePrimaryHttpMessageHandler(() => new StaticHandler(upstream)));
        await using var factory = harness.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Blog:MarketingBaseUrl"] = "https://receiver.test" })));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        using var response = await client.PostAsJsonAsync("/api/public/inquiries", new { submissionId = "test" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TimeSpan.FromSeconds(12), response.Headers.RetryAfter?.Delta);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Contact is required.", body.GetProperty("errors").GetProperty("contact")[0].GetString());
        Assert.Equal("marketing-trace", body.GetProperty("upstreamTraceId").GetString());
        Assert.Equal("marketing-request", body.GetProperty("upstreamRequestId").GetString());
        Assert.NotEqual("marketing-trace", body.GetProperty("traceId").GetString());
        Assert.Equal("/api/public/inquiries", body.GetProperty("instance").GetString());
    }

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
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Blog:MarketingBaseUrl"] = origin }).Build();
        return new(configuration, new Factory(handler))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
            { RequestServices = new ServiceCollection().AddLogging().AddControllers().Services.BuildServiceProvider() } }
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
            return new(HttpStatusCode.Accepted) { Content = JsonContent.Create(new { reference = "synthetic-reference" }) };
        }
    }

    private sealed class CancelledHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromCanceled<HttpResponseMessage>(ct);
    }
}
