using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
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
