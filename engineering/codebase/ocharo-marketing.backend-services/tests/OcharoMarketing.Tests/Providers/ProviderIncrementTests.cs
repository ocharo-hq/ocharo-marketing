using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Infrastructure.Providers;
using OcharoMarketing.Infrastructure.Providers.Brokers;
using Xunit;

namespace OcharoMarketing.Tests.Providers;

public sealed class ProviderIncrementTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-27T00:00:00Z", CultureInfo.InvariantCulture);
    private static readonly byte[] Image = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j9t8AAAAASUVORK5CYII=");
    private static readonly CancellationToken Token = CancellationToken.None;

    [Fact]
    public async Task Verify_ShouldAvoidReads_WhenProvidersAreDisabled()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        IProviderVerificationBroker[] brokers =
        [
            new TelegramAccountVerificationBroker(client, new() { BotToken = "123:fake_secret" }),
            new XAccountVerificationBroker(client, new() { AccessToken = "fake_secret", UserId = "123" })
        ];
        foreach (var broker in brokers)
            Assert.Equal(BrokerOutcome.Rejected, (await broker.VerifyAsync(new("123"), Token)).Outcome);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Verify_ShouldResolveNumericChannelAndBot_WhenTelegramPostingPermissionExists()
    {
        using var handler = TelegramReads();
        using var client = new HttpClient(handler);
        var result = await new TelegramAccountVerificationBroker(client, TelegramOptions(), new FixedClock())
            .VerifyAsync(new("@test_channel", "123"), Token);

        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal("123", result.Identity?.AccountId);
        Assert.Equal("-100123", result.Identity?.DestinationId);
        Assert.Equal("Test channel", result.Identity?.DisplayName);
        Assert.Equal(Now, result.Identity?.VerifiedAt);
        Assert.Equal(new[] { "text", "photo" }, result.Identity?.Capabilities);
        Assert.Equal(3, handler.Requests.Count);
        Assert.All(handler.Requests, sent => Assert.Equal("GET", sent.Method));
        Assert.EndsWith("/getMe", handler.Requests[0].Uri);
        Assert.Contains("chat_id=%40test_channel", handler.Requests[1].Uri);
        Assert.EndsWith("getChatMember?chat_id=-100123&user_id=123", handler.Requests[2].Uri);
    }

    [Theory]
    [InlineData("account", 1, "account_mismatch")]
    [InlineData("destination", 2, "destination_mismatch")]
    [InlineData("permission", 3, "posting_permission_missing")]
    [InlineData("member", 3, "member_mismatch")]
    [InlineData("private", 2, "channel_required")]
    public async Task Verify_ShouldRejectFalseIdentityEvidence_WhenTelegramReadDoesNotMatch(string failure, int reads, string code)
    {
        using var handler = TelegramReads(failure);
        using var client = new HttpClient(handler);
        var result = await new TelegramAccountVerificationBroker(client, TelegramOptions(), new FixedClock())
            .VerifyAsync(new("-100123", "123"), Token);

        Assert.NotEqual(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Null(result.Identity);
        Assert.Equal(code, result.Code);
        Assert.Equal(reads, handler.Requests.Count);
    }

    [Theory]
    [InlineData("@test&other")]
    [InlineData("-00100123")]
    [InlineData("4503599627370496")]
    public async Task Verify_ShouldRejectUnboundedOrNonCanonicalTelegramIdentity_WithoutReads(string destination)
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var result = await new TelegramAccountVerificationBroker(client, TelegramOptions())
            .VerifyAsync(new(destination), Token);
        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Verify_ShouldSupportReadOnlyIdentity_WhenXMetricReadsAreEnabledWithoutPublishing()
    {
        using var handler = new FakeHandler(_ => Json("{\"data\":{\"id\":\"123\",\"name\":\"Test account\",\"username\":\"test\"}}"));
        using var client = new HttpClient(handler);
        var result = await new XAccountVerificationBroker(client, XOptions(publish: false), new FixedClock())
            .VerifyAsync(new("123", "123"), Token);

        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal("123", result.Identity?.AccountId);
        Assert.Equal("123", result.Identity?.DestinationId);
        Assert.Equal(new[] { "public-metrics" }, result.Identity?.Capabilities);
        var sent = Assert.Single(handler.Requests);
        Assert.Equal("GET", sent.Method);
        Assert.Equal("https://api.x.com/2/users/me", sent.Uri);
        Assert.Equal("Bearer fake_secret", sent.Authorization);
    }

    [Theory]
    [InlineData("{\"data\":{\"id\":\"456\"}}", "account_mismatch")]
    [InlineData("{\"data\":{\"id\":\"123\"},\"errors\":[{\"detail\":\"fake_secret\"}]}", "identity_unavailable")]
    [InlineData("[]", "identity_unavailable")]
    public async Task Verify_ShouldRefuseMismatchOrPartialIdentity_WhenXReplies(string response, string code)
    {
        using var handler = new FakeHandler(_ => Json(response));
        using var client = new HttpClient(handler);
        var result = await new XAccountVerificationBroker(client, XOptions()).VerifyAsync(new("123"), Token);
        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Equal(code, result.Code);
        Assert.Null(result.Identity);
        Assert.DoesNotContain("fake_secret", result.Message);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Read_ShouldPreserveActualZeroAndMissingCounters_WhenOwnedPostIsReturned()
    {
        using var handler = new FakeHandler(_ => Json("{\"data\":{\"id\":\"201\",\"author_id\":\"123\",\"public_metrics\":{\"like_count\":0,\"impression_count\":42}}}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(client, XOptions(publish: false), new FixedClock()).ReadAsync(Metrics(), Token);

        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal(0m, result.Observations.Single(item => item.Name == "likes").Value);
        Assert.Equal(Now, result.Observations.Single(item => item.Name == "likes").ObservedAt);
        Assert.Equal(42m, result.Observations.Single(item => item.Name == "impressions").Value);
        Assert.Null(result.Observations.Single(item => item.Name == "replies").Value);
        Assert.Null(result.Observations.Single(item => item.Name == "replies").ObservedAt);
        Assert.Equal("not_returned", result.Observations.Single(item => item.Name == "replies").Availability);
        var sent = Assert.Single(handler.Requests);
        Assert.Equal("GET", sent.Method);
        Assert.EndsWith("/2/tweets/201?tweet.fields=author_id,public_metrics", sent.Uri);
    }

    [Fact]
    public async Task Read_ShouldRetainAvailableCounters_WhenXReturnsPartialErrors()
    {
        using var handler = new FakeHandler(_ => Json("{\"data\":{\"id\":\"201\",\"author_id\":\"123\",\"public_metrics\":{\"like_count\":4}},\"errors\":[{\"detail\":\"fake_secret\"}]}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(client, XOptions(), new FixedClock()).ReadAsync(Metrics(), Token);
        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.True(result.HasPartialErrors);
        Assert.Equal("partial_metrics", result.Code);
        Assert.Equal(4m, result.Observations.Single(item => item.Name == "likes").Value);
        Assert.DoesNotContain("fake_secret", result.Message);
        Assert.All(result.Observations, item => Assert.DoesNotContain("fake_secret", item.Reason ?? ""));
    }

    [Theory]
    [InlineData("202", "123")]
    [InlineData("201", "456")]
    [InlineData("201", null)]
    public async Task Read_ShouldRejectCounters_WhenPostOrAuthorIdentityDiffers(string id, string? author)
    {
        using var handler = new FakeHandler(_ => Json(JsonSerializer.Serialize(new { data = new { id, author_id = author, public_metrics = new { like_count = 100 } } })));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(client, XOptions()).ReadAsync(Metrics(), Token);
        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Equal("identity_mismatch", result.Code);
        Assert.All(result.Observations, item => { Assert.Null(item.Value); Assert.Null(item.ObservedAt); });
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.2")]
    [InlineData("\"4\"")]
    [InlineData("9007199254740992")]
    [InlineData("1e500")]
    public async Task Read_ShouldLeaveInvalidCountersUnavailable_WithoutClamping(string value)
    {
        using var handler = new FakeHandler(_ => Json("{\"data\":{\"id\":\"201\",\"author_id\":\"123\",\"public_metrics\":{\"like_count\":" + value + "}}}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(client, XOptions()).ReadAsync(Metrics() with { Metrics = ["likes"] }, Token);
        var observation = Assert.Single(result.Observations);
        Assert.Null(observation.Value);
        Assert.Null(observation.ObservedAt);
        Assert.Equal("invalid_value", observation.Availability);
    }

    [Fact]
    public async Task Read_ShouldRejectBeforeSending_WhenMetricOptInOrReceiptIdentityIsMissing()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var disabled = await new XSocialMetricsBroker(client, XOptions(read: false)).ReadAsync(Metrics(), Token);
        Assert.Equal("not_enabled", disabled.Code);
        var broker = new XSocialMetricsBroker(client, XOptions());
        SocialMetricsRequest[] invalid =
        [
            Metrics() with { AccountId = "456" },
            Metrics() with { Receipt = new("telegram", "published", "201") },
            Metrics() with { Receipt = new("x", "publication_unknown", "201") },
            Metrics() with { Receipt = new("x", "published", "../201") },
            Metrics() with { Receipt = new("x", "published", "201", new Dictionary<string, string> { ["account_id"] = "456" }) }
        ];
        foreach (var request in invalid)
            Assert.Equal(BrokerOutcome.Rejected, (await broker.ReadAsync(request, Token)).Outcome);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Read_ShouldRejectUnsupportedNames_WithoutPaidReads()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(client, XOptions()).ReadAsync(Metrics() with { Metrics = ["reach"] }, Token);
        Assert.Equal(BrokerOutcome.Unsupported, result.Outcome);
        Assert.Equal("not_supported", Assert.Single(result.Observations).Availability);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Read_ShouldPreservePermissionAvailability_AndRedactProviderDiagnostics()
    {
        using var handler = new FakeHandler(_ => Json("{\"detail\":\"fake_secret\"}", HttpStatusCode.Forbidden));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(client, XOptions()).ReadAsync(Metrics(), Token);
        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.All(result.Observations, item => { Assert.Null(item.Value); Assert.Equal("permission_missing", item.Availability); });
        Assert.DoesNotContain("fake_secret", result.Message);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Read_ShouldPreservePartialRefusal_WhenNoPostDataIsReturned()
    {
        using var handler = new FakeHandler(_ => Json("{\"errors\":[{\"detail\":\"fake_secret\"}]}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(client, XOptions()).ReadAsync(Metrics(), Token);
        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.True(result.HasPartialErrors);
        Assert.All(result.Observations, item => Assert.Null(item.Value));
        Assert.DoesNotContain("fake_secret", result.Message);
    }

    [Fact]
    public async Task Publish_ShouldRequireNumericBindingsAndCheckpoint_BeforeTelegramSend()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = new TelegramSocialPublishingBroker(client, TelegramOptions());
        var missing = await broker.PublishAsync(new("-100123", "Approved", AccountId: "123"), Token);
        Assert.Equal("checkpoint_required", missing.Code);
        foreach (var request in new[]
        {
            TelegramRequest() with { AccountId = "456" }, TelegramRequest() with { DestinationId = "@test_channel" },
            TelegramRequest() with { DestinationId = "-100456" }
        })
            Assert.Equal("identity_mismatch", (await broker.PublishAsync(request, Token)).Code);
        var failed = await broker.PublishAsync(TelegramRequest() with { SaveCheckpointAsync = (_, _) => throw new IOException("fake_secret") }, Token);
        Assert.Equal(BrokerOutcome.Rejected, failed.Outcome);
        Assert.Equal("checkpoint_failed", failed.Code);
        Assert.DoesNotContain("fake_secret", failed.Message);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Publish_ShouldCheckpointBeforeSend_AndMatchTelegramReturnedChannel()
    {
        ProviderReceipt? saved = null;
        using var handler = new FakeHandler(_ =>
        {
            Assert.Equal("publication_started", saved?.Stage);
            Assert.Equal("123", saved?.RelatedIds?["account_id"]);
            return Json("{\"ok\":true,\"result\":{\"message_id\":71,\"chat\":{\"id\":-100123}}}");
        });
        using var client = new HttpClient(handler);
        var result = await new TelegramSocialPublishingBroker(client, TelegramOptions()).PublishAsync(
            TelegramRequest() with { SaveCheckpointAsync = (receipt, _) => { saved = receipt; return Task.CompletedTask; } }, Token);
        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal("published", result.Receipt?.Stage);
        Assert.Equal("-100123", result.Receipt?.RelatedIds?["chat_id"]);
        Assert.Equal("123", result.Receipt?.RelatedIds?["account_id"]);
    }

    [Theory]
    [InlineData("{\"ok\":true,\"result\":{\"message_id\":71,\"chat\":{\"id\":-100456}}}", "destination_mismatch")]
    [InlineData("{\"ok\":true,\"result\":{\"message_id\":71}}", "missing_receipt")]
    [InlineData("[]", "missing_receipt")]
    public async Task Publish_ShouldKeepUnknown_WhenTelegramReceiptIsWrongOrIncomplete(string response, string code)
    {
        using var handler = new FakeHandler(_ => Json(response));
        using var client = new HttpClient(handler);
        var broker = new TelegramSocialPublishingBroker(client, TelegramOptions());
        var result = await broker.PublishAsync(TelegramRequest(), Token);
        Assert.Equal(BrokerOutcome.Ambiguous, result.Outcome);
        Assert.Equal(code, result.Code);
        Assert.Equal("publication_unknown", result.Receipt?.Stage);
        Assert.Equal(BrokerOutcome.Unsupported, (await broker.PublishAsync(TelegramRequest() with { ResumeReceipt = result.Receipt }, Token)).Outcome);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Publish_ShouldNotSend_WhenXTextDispatchCheckpointFails()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialPublishingBroker(client, XOptions(), new FixedClock()).PublishAsync(
            XRequest() with { SaveCheckpointAsync = (_, _) => throw new IOException("fake_secret") }, Token);
        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Equal("checkpoint_failed", result.Code);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("0")]
    [InlineData("\"3600\"")]
    public async Task Publish_ShouldAvoidFinalPost_WhenXUploadExpiryIsMissingOrInvalid(string expiry)
    {
        using var handler = new FakeHandler(_ => Json("{\"data\":{\"id\":\"101\",\"expires_after_secs\":" + expiry + "}}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialPublishingBroker(client, XOptions(), new FixedClock()).PublishAsync(
            XRequest() with { Media = [new(Image, "image/png")] }, Token);
        Assert.Equal(BrokerOutcome.Ambiguous, result.Outcome);
        Assert.Equal("upload_expiry_unavailable", result.Code);
        Assert.Equal("media_upload_unknown", result.Receipt?.Stage);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Publish_ShouldAvoidFinalPostAndReplay_WhenXUploadHasPartialErrors()
    {
        using var handler = new FakeHandler(_ => Json("{\"data\":{\"id\":\"101\",\"expires_after_secs\":3600},\"errors\":[{\"detail\":\"fake_secret\"}]}"));
        using var client = new HttpClient(handler);
        var broker = new XSocialPublishingBroker(client, XOptions(), new FixedClock());
        var input = XRequest() with { Media = [new(Image, "image/png")] };
        var result = await broker.PublishAsync(input, Token);
        Assert.Equal(BrokerOutcome.Ambiguous, result.Outcome);
        Assert.Equal("partial_upload", result.Code);
        Assert.Equal("media_upload_unknown", result.Receipt?.Stage);
        Assert.Equal(BrokerOutcome.Unsupported, (await broker.PublishAsync(input with { ResumeReceipt = result.Receipt }, Token)).Outcome);
        Assert.Single(handler.Requests);
        Assert.DoesNotContain("fake_secret", result.Message);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("missing")]
    [InlineData("wrong_account")]
    [InlineData("wrong_image")]
    public async Task Publish_ShouldRejectUnsafeUploadResume_WithoutSending(string failure)
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var related = UploadIds();
        if (failure == "expired") related["expires_at"] = Now.ToString("O", CultureInfo.InvariantCulture);
        if (failure == "missing") related.Remove("expires_at");
        if (failure == "wrong_account") related["account_id"] = "456";
        if (failure == "wrong_image") related["media_sha256"] = "different";
        var result = await new XSocialPublishingBroker(client, XOptions(), new FixedClock()).PublishAsync(
            XRequest() with { Media = [new(Image, "image/png")], ResumeReceipt = new("x", "media_uploaded", "101", related) }, Token);
        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Equal("upload_unverified_or_expired", result.Code);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Publish_ShouldResumeOnlyUnexpiredOwnedMedia_WithoutSecondUpload()
    {
        var stages = new List<string>();
        using var handler = new FakeHandler(_ =>
        {
            Assert.Equal(new[] { "publication_started" }, stages);
            return Json("{\"data\":{\"id\":\"201\"}}");
        });
        using var client = new HttpClient(handler);
        var result = await new XSocialPublishingBroker(client, XOptions(), new FixedClock()).PublishAsync(
            XRequest() with
            {
                Media = [new(Image, "image/png")], ResumeReceipt = new("x", "media_uploaded", "101", UploadIds()),
                SaveCheckpointAsync = (receipt, _) => { stages.Add(receipt.Stage); return Task.CompletedTask; }
            }, Token);
        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal("123", result.Receipt?.RelatedIds?["account_id"]);
        Assert.EndsWith("/2/tweets", Assert.Single(handler.Requests).Uri);
    }

    [Theory]
    [InlineData("{\"data\":{\"id\":\"201\"},\"errors\":[{\"detail\":\"fake_secret\"}]}")]
    [InlineData("{\"data\":{\"id\":\"201\",\"author_id\":\"456\"}}")]
    public async Task Publish_ShouldNotClaimSuccess_WhenXFinalReceiptHasConflictingEvidence(string response)
    {
        using var handler = new FakeHandler(_ => Json(response));
        using var client = new HttpClient(handler);
        var result = await new XSocialPublishingBroker(client, XOptions(), new FixedClock()).PublishAsync(XRequest(), Token);
        Assert.Equal(BrokerOutcome.Ambiguous, result.Outcome);
        Assert.Equal("publication_unknown", result.Receipt?.Stage);
        Assert.Equal("123", result.Receipt?.RelatedIds?["account_id"]);
        Assert.Single(handler.Requests);
        Assert.DoesNotContain("fake_secret", result.Message);
    }

    [Fact]
    public async Task Publish_ShouldBindTextOnlyXReceiptToAccount_AfterDurableDispatch()
    {
        ProviderReceipt? saved = null;
        using var handler = new FakeHandler(_ =>
        {
            Assert.Equal("publication_started", saved?.Stage);
            return Json("{\"data\":{\"id\":\"201\"}}");
        });
        using var client = new HttpClient(handler);
        var result = await new XSocialPublishingBroker(client, XOptions(), new FixedClock()).PublishAsync(
            XRequest() with { SaveCheckpointAsync = (receipt, _) => { saved = receipt; return Task.CompletedTask; } }, Token);
        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal("201", result.Receipt?.ExternalId);
        Assert.Equal("123", result.Receipt?.RelatedIds?["account_id"]);
    }

    private static SocialMetricsRequest Metrics() => new("123", new("x", "published", "201"));
    private static SocialPublishRequest XRequest()
        => new("123", "Approved", SaveCheckpointAsync: (_, _) => Task.CompletedTask, AccountId: "123");
    private static SocialPublishRequest TelegramRequest()
        => new("-100123", "Approved", SaveCheckpointAsync: (_, _) => Task.CompletedTask, AccountId: "123");
    private static TelegramProviderOptions TelegramOptions()
        => new() { Enabled = true, BotToken = "123:fake_secret", BotId = "123", DestinationChatId = "-100123" };
    private static XProviderOptions XOptions(bool publish = true, bool read = true)
        => new() { Enabled = publish, EnableMetricReads = read, AccessToken = "fake_secret", UserId = "123" };
    private static Dictionary<string, string> UploadIds()
        => new() { ["account_id"] = "123", ["expires_at"] = Now.AddHours(1).ToString("O", CultureInfo.InvariantCulture),
            ["media_sha256"] = Convert.ToHexStringLower(SHA256.HashData(Image)) };

    private static FakeHandler TelegramReads(string? failure = null) => new(sent =>
    {
        if (sent.Uri.EndsWith("/getMe"))
            return Json(failure == "account" ? "{\"ok\":true,\"result\":{\"id\":456,\"is_bot\":true}}"
                : "{\"ok\":true,\"result\":{\"id\":123,\"is_bot\":true}}");
        if (sent.Uri.Contains("/getChat?"))
            return Json(failure == "destination" ? "{\"ok\":true,\"result\":{\"id\":-100456,\"type\":\"channel\"}}"
                : failure == "private" ? "{\"ok\":true,\"result\":{\"id\":123,\"type\":\"private\"}}"
                : "{\"ok\":true,\"result\":{\"id\":-100123,\"type\":\"channel\",\"title\":\"Test channel\",\"username\":\"test_channel\"}}");
        return Json(failure == "permission"
            ? "{\"ok\":true,\"result\":{\"status\":\"administrator\",\"can_post_messages\":false,\"user\":{\"id\":123,\"is_bot\":true}}}"
            : failure == "member"
                ? "{\"ok\":true,\"result\":{\"status\":\"administrator\",\"can_post_messages\":true,\"user\":{\"id\":456,\"is_bot\":true}}}"
                : "{\"ok\":true,\"result\":{\"status\":\"administrator\",\"can_post_messages\":true,\"user\":{\"id\":123,\"is_bot\":true}}}");
    });

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed record CapturedRequest(string Method, string Uri, string Body, string? Authorization);
    private sealed class FakeHandler(Func<CapturedRequest, HttpResponseMessage> response) : HttpMessageHandler
    {
        internal List<CapturedRequest> Requests { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var captured = new CapturedRequest(request.Method.Method, request.RequestUri!.ToString(),
                request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken),
                request.Headers.Authorization?.ToString());
            Requests.Add(captured);
            return response(captured);
        }
    }
}
