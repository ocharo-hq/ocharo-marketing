using System.Net;
using System.Text;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Infrastructure.Providers;
using OcharoMarketing.Infrastructure.Providers.Brokers;
using Xunit;

namespace OcharoMarketing.Tests.Providers;

public sealed class ReceiptReconciliationTests
{
    private static readonly CancellationToken Token = CancellationToken.None;

    [Fact]
    public async Task Reconcile_ShouldVerifyExistingOwnedPost_WithoutPublication()
    {
        using var handler = new FakeHandler("{\"data\":{\"id\":\"201\",\"author_id\":\"123\"}}");
        using var client = new HttpClient(handler);
        var result = await new XReceiptReconciliationBroker(client, Options())
            .ReconcileAsync(new("123", Receipt()), Token);

        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal("published", result.Receipt?.Stage);
        Assert.Equal("201", result.Receipt?.ExternalId);
        Assert.Equal("123", result.Receipt?.RelatedIds?["account_id"]);
        Assert.Equal("201", result.Receipt?.RelatedIds?["post_id"]);
        Assert.Equal("GET", Assert.Single(handler.Requests).Method);
        Assert.Equal("https://api.x.com/2/tweets/201?tweet.fields=author_id", handler.Requests[0].Uri);
    }

    [Fact]
    public async Task Reconcile_ShouldAcceptPublishedReceipt_WhenAccountBindingExists()
    {
        using var handler = new FakeHandler("{\"data\":{\"id\":\"201\",\"author_id\":\"123\"}}");
        using var client = new HttpClient(handler);
        var receipt = new ProviderReceipt("x", "published", "201", new Dictionary<string, string> { ["account_id"] = "123" });
        var result = await new XReceiptReconciliationBroker(client, Options()).ReconcileAsync(new("123", receipt), Token);
        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("{\"data\":{\"id\":\"202\",\"author_id\":\"123\"}}")]
    [InlineData("{\"data\":{\"id\":\"201\",\"author_id\":\"456\"}}")]
    [InlineData("{\"data\":{\"id\":\"201\"}}")]
    public async Task Reconcile_ShouldRejectIdentityMismatch_WithoutVerifiedReceipt(string response)
    {
        using var handler = new FakeHandler(response);
        using var client = new HttpClient(handler);
        var result = await new XReceiptReconciliationBroker(client, Options()).ReconcileAsync(new("123", Receipt()), Token);
        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Equal("identity_mismatch", result.Code);
        Assert.Null(result.Receipt);
        Assert.Equal("GET", Assert.Single(handler.Requests).Method);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"errors\":[{\"detail\":\"fake_secret\"}]}")]
    [InlineData("{\"data\":{\"id\":\"201\",\"author_id\":\"123\"},\"errors\":[{\"detail\":\"fake_secret\"}]}")]
    public async Task Reconcile_ShouldKeepIncompleteOrPartialEvidenceUnresolved(string response)
    {
        using var handler = new FakeHandler(response);
        using var client = new HttpClient(handler);
        var result = await new XReceiptReconciliationBroker(client, Options()).ReconcileAsync(new("123", Receipt()), Token);
        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Equal("receipt_unavailable", result.Code);
        Assert.Null(result.Receipt);
        Assert.DoesNotContain("fake_secret", result.Message);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("publication_started", "201", true)]
    [InlineData("media_uploaded", "201", true)]
    [InlineData("media_upload_unknown", "201", true)]
    [InlineData("publication_unknown", "123", false)]
    [InlineData("publication_unknown", "", false)]
    [InlineData("publication_unknown", "not-a-post", true)]
    public async Task Reconcile_ShouldNotInventPostLookup_WhenOnlyDispatchOrUploadMarkerExists(
        string stage, string id, bool includePostId)
    {
        using var handler = new FakeHandler("{}");
        using var client = new HttpClient(handler);
        var ids = new Dictionary<string, string> { ["account_id"] = "123" };
        if (includePostId) ids["post_id"] = id;
        var result = await new XReceiptReconciliationBroker(client, Options())
            .ReconcileAsync(new("123", new("x", stage, id, ids)), Token);
        Assert.Equal(BrokerOutcome.Unsupported, result.Outcome);
        Assert.Equal("known_post_required", result.Code);
        Assert.Null(result.Receipt);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("source")]
    [InlineData("provider")]
    [InlineData("receipt-account")]
    [InlineData("post-id")]
    public async Task Reconcile_ShouldRejectStoredIdentityConflicts_BeforeRead(string mismatch)
    {
        using var handler = new FakeHandler("{}");
        using var client = new HttpClient(handler);
        var receipt = Receipt() with
        {
            Provider = mismatch == "provider" ? "telegram" : "x",
            Stage = "published",
            RelatedIds = new Dictionary<string, string>
            {
                ["account_id"] = mismatch == "receipt-account" ? "456" : "123",
                ["post_id"] = mismatch == "post-id" ? "202" : "201"
            }
        };
        var result = await new XReceiptReconciliationBroker(client, Options())
            .ReconcileAsync(new(mismatch == "source" ? "456" : "123", receipt), Token);
        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Equal("receipt_mismatch", result.Code);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Reconcile_ShouldRequireExplicitReadEnablement_EvenWhenPublishingEnabled()
    {
        using var handler = new FakeHandler("{}");
        using var client = new HttpClient(handler);
        var options = new XProviderOptions { Enabled = true, EnableMetricReads = false, UserId = "123", AccessToken = "fake_secret" };
        var result = await new XReceiptReconciliationBroker(client, options).ReconcileAsync(new("123", Receipt()), Token);
        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Equal("not_enabled", result.Code);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Reconcile_ShouldPreserveReadFailure_WithoutPublication()
    {
        using var handler = new FakeHandler("{\"detail\":\"fake_secret\"}", HttpStatusCode.NotFound);
        using var client = new HttpClient(handler);
        var result = await new XReceiptReconciliationBroker(client, Options()).ReconcileAsync(new("123", Receipt()), Token);
        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Equal("http_404", result.Code);
        Assert.Null(result.Receipt);
        Assert.DoesNotContain("fake_secret", result.Message);
        Assert.Equal("GET", Assert.Single(handler.Requests).Method);
    }

    [Fact]
    public async Task Reconcile_ShouldReportUnsupportedTelegramLookup_WithoutPretendingSuccess()
    {
        var result = await new TelegramReceiptReconciliationBroker().ReconcileAsync(
            new("123", new("telegram", "publication_unknown", "-100123")), Token);
        Assert.Equal(BrokerOutcome.Unsupported, result.Outcome);
        Assert.Equal("receipt_lookup_unavailable", result.Code);
        Assert.Null(result.Receipt);
    }

    private static XProviderOptions Options()
        => new() { Enabled = false, EnableMetricReads = true, UserId = "123", AccessToken = "fake_secret" };
    private static ProviderReceipt Receipt()
        => new("x", "publication_unknown", "201", new Dictionary<string, string> { ["account_id"] = "123", ["post_id"] = "201" });
    private sealed class FakeHandler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        internal List<(string Method, string Uri)> Requests { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method.Method, request.RequestUri!.ToString()));
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
