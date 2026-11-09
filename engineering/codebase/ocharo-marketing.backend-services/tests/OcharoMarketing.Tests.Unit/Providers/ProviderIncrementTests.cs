using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using AwesomeAssertions;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Infrastructure.Providers;
using OcharoMarketing.Infrastructure.Providers.Brokers;
using Xunit;

namespace OcharoMarketing.Tests.Unit.Providers;

public sealed class ProviderIncrementTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse(
        "2026-09-27T00:00:00Z",
        CultureInfo.InvariantCulture);
    private static readonly byte[] Image = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j9t8AAAAASUVORK5CYII=");
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
            ((await broker.VerifyAsync(new("123"), Token)).Outcome).Should().Be(BrokerOutcome.Rejected);
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Verify_ShouldResolveNumericChannelAndBot_WhenTelegramPostingPermissionExists()
    {
        using var handler = TelegramReads();
        using var client = new HttpClient(handler);
        var result = await new TelegramAccountVerificationBroker(client, TelegramOptions(), new FakeTimeProvider(Now))
            .VerifyAsync(new("@test_channel", "123"), Token);

        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        (result.Identity?.AccountId).Should().Be("123");
        (result.Identity?.DestinationId).Should().Be("-100123");
        (result.Identity?.DisplayName).Should().Be("Test channel");
        (result.Identity?.VerifiedAt).Should().Be(Now);
        (result.Identity?.Capabilities).Should().Equal(new[] { "text", "photo" });
        handler.Requests.Count.Should().Be(3);
        handler.Requests.Should().AllSatisfy(sent => sent.Method.Should().Be("GET"));
        handler.Requests[0].Uri.Should().EndWith("/getMe");
        handler.Requests[1].Uri.Should().Contain("chat_id=%40test_channel");
        handler.Requests[2].Uri.Should().EndWith("getChatMember?chat_id=-100123&user_id=123");
    }

    [Theory]
    [InlineData("account", 1, "account_mismatch")]
    [InlineData("destination", 2, "destination_mismatch")]
    [InlineData("permission", 3, "posting_permission_missing")]
    [InlineData("member", 3, "member_mismatch")]
    [InlineData("private", 2, "channel_required")]
    public async Task Verify_ShouldRejectFalseIdentityEvidence_WhenTelegramReadDoesNotMatch(
        string failure,
        int reads,
        string code)
    {
        using var handler = TelegramReads(failure);
        using var client = new HttpClient(handler);
        var result = await new TelegramAccountVerificationBroker(client, TelegramOptions(), new FakeTimeProvider(Now))
            .VerifyAsync(new("-100123", "123"), Token);

        result.Outcome.Should().NotBe(BrokerOutcome.Succeeded);
        result.Identity.Should().BeNull();
        result.Code.Should().Be(code);
        handler.Requests.Count.Should().Be(reads);
    }

    [Theory]
    [InlineData("@test&other")]
    [InlineData("-00100123")]
    [InlineData("4503599627370496")]
    public async Task Verify_ShouldRejectWithoutReads_WhenTheTelegramIdentityIsUnboundedOrNonCanonical(
        string destination)
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var result = await new TelegramAccountVerificationBroker(client, TelegramOptions())
            .VerifyAsync(new(destination), Token);
        result.Outcome.Should().Be(BrokerOutcome.Rejected);
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Verify_ShouldSupportReadOnlyIdentity_WhenXMetricReadsAreEnabledWithoutPublishing()
    {
        using var handler = new FakeHandler(
            _ => Json("{\"data\":{\"id\":\"123\",\"name\":\"Test account\",\"username\":\"test\"}}"));
        using var client = new HttpClient(handler);
        var result = await new XAccountVerificationBroker(client, XOptions(publish: false), new FakeTimeProvider(Now))
            .VerifyAsync(new("123", "123"), Token);

        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        (result.Identity?.AccountId).Should().Be("123");
        (result.Identity?.DestinationId).Should().Be("123");
        (result.Identity?.Capabilities).Should().Equal(new[] { "public-metrics" });
        var sent = handler.Requests.Should().ContainSingle().Which;
        sent.Method.Should().Be("GET");
        sent.Uri.Should().Be("https://api.x.com/2/users/me");
        sent.Authorization.Should().Be("Bearer fake_secret");
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
        result.Outcome.Should().Be(BrokerOutcome.Rejected);
        result.Code.Should().Be(code);
        result.Identity.Should().BeNull();
        result.Message.Should().NotContain("fake_secret");
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task Read_ShouldPreserveActualZeroAndMissingCounters_WhenOwnedPostIsReturned()
    {
        using var handler = new FakeHandler(
            _ =>
                Json(
                    "{\"data\":{\"id\":\"201\",\"author_id\":\"123\",\"public_metrics\":{\"like_count\":0,\"imp"
                        + "ression_count\":42}}}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(
            client,
            XOptions(publish: false),
            new FakeTimeProvider(Now)).ReadAsync(Metrics(), Token);

        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        (result.Observations.Single(item => item.Name == "likes").Value).Should().Be(0m);
        (result.Observations.Single(item => item.Name == "likes").ObservedAt).Should().Be(Now);
        (result.Observations.Single(item => item.Name == "impressions").Value).Should().Be(42m);
        (result.Observations.Single(item => item.Name == "replies").Value).Should().BeNull();
        (result.Observations.Single(item => item.Name == "replies").ObservedAt).Should().BeNull();
        (result.Observations.Single(item => item.Name == "replies").Availability).Should().Be("not_returned");
        var sent = handler.Requests.Should().ContainSingle().Which;
        sent.Method.Should().Be("GET");
        sent.Uri.Should().EndWith("/2/tweets/201?tweet.fields=author_id,public_metrics");
    }

    [Fact]
    public async Task Read_ShouldRetainAvailableCounters_WhenXReturnsPartialErrors()
    {
        using var handler = new FakeHandler(
            _ =>
                Json(
                    "{\"data\":{\"id\":\"201\",\"author_id\":\"123\",\"public_metrics\":{\"like_count\":4}},\"e"
                        + "rrors\":[{\"detail\":\"fake_secret\"}]}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(
            client,
            XOptions(),
            new FakeTimeProvider(Now)).ReadAsync(Metrics(), Token);
        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        result.HasPartialErrors.Should().BeTrue();
        result.Code.Should().Be("partial_metrics");
        (result.Observations.Single(item => item.Name == "likes").Value).Should().Be(4m);
        result.Message.Should().NotContain("fake_secret");
        result.Observations.Should().AllSatisfy(item => (item.Reason ?? "").Should().NotContain("fake_secret"));
    }

    [Theory]
    [InlineData("202", "123")]
    [InlineData("201", "456")]
    [InlineData("201", null)]
    public async Task Read_ShouldRejectCounters_WhenPostOrAuthorIdentityDiffers(string id, string? author)
    {
        using var handler = new FakeHandler(
            _ =>
                Json(
                    JsonSerializer.Serialize(
                        new { data = new { id, author_id = author, public_metrics = new { like_count = 100 } } })));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(client, XOptions()).ReadAsync(Metrics(), Token);
        result.Outcome.Should().Be(BrokerOutcome.Rejected);
        result.Code.Should().Be("identity_mismatch");
        result.Observations
            .Should()
            .AllSatisfy(item => { item.Value.Should().BeNull(); item.ObservedAt.Should().BeNull(); });
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.2")]
    [InlineData("\"4\"")]
    [InlineData("9007199254740992")]
    [InlineData("1e500")]
    public async Task Read_ShouldLeaveCountersUnavailableWithoutClamping_WhenTheyAreInvalid(string value)
    {
        using var handler = new FakeHandler(
            _ =>
                Json(
                    "{\"data\":{\"id\":\"201\",\"author_id\":\"123\",\"public_metrics\":{\"like_count\":"
                        + value
                        + "}}}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(
            client,
            XOptions()).ReadAsync(Metrics() with { Metrics = ["likes"] }, Token);
        var observation = result.Observations.Should().ContainSingle().Which;
        observation.Value.Should().BeNull();
        observation.ObservedAt.Should().BeNull();
        observation.Availability.Should().Be("invalid_value");
    }

    [Fact]
    public async Task Read_ShouldRejectBeforeSending_WhenMetricOptInOrReceiptIdentityIsMissing()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var disabled = await new XSocialMetricsBroker(client, XOptions(read: false)).ReadAsync(Metrics(), Token);
        disabled.Code.Should().Be("not_enabled");
        var broker = new XSocialMetricsBroker(client, XOptions());
        SocialMetricsRequest[] invalid =
        [
            Metrics() with { AccountId = "456" },
            Metrics() with { Receipt = new("telegram", "published", "201") },
            Metrics() with { Receipt = new("x", "publication_unknown", "201") },
            Metrics() with { Receipt = new("x", "published", "../201") },
            Metrics() with
            {
                Receipt = new("x", "published", "201", new Dictionary<string, string> { ["account_id"] = "456" })
            }
        ];
        foreach (var request in invalid)
            ((await broker.ReadAsync(request, Token)).Outcome).Should().Be(BrokerOutcome.Rejected);
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Read_ShouldRejectWithoutPaidReads_WhenNamesAreUnsupported()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(
            client,
            XOptions()).ReadAsync(Metrics() with { Metrics = ["reach"] }, Token);
        result.Outcome.Should().Be(BrokerOutcome.Unsupported);
        result.Observations.Should().ContainSingle().Which.Availability.Should().Be("not_supported");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Read_ShouldPreservePermissionAvailabilityAndRedactDiagnostics()
    {
        using var handler = new FakeHandler(_ => Json("{\"detail\":\"fake_secret\"}", HttpStatusCode.Forbidden));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(client, XOptions()).ReadAsync(Metrics(), Token);
        result.Outcome.Should().Be(BrokerOutcome.Rejected);
        result.Observations
            .Should()
            .AllSatisfy(item => { item.Value.Should().BeNull(); item.Availability.Should().Be("permission_missing"); });
        result.Message.Should().NotContain("fake_secret");
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task Read_ShouldPreservePartialRefusal_WhenNoPostDataIsReturned()
    {
        using var handler = new FakeHandler(_ => Json("{\"errors\":[{\"detail\":\"fake_secret\"}]}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialMetricsBroker(client, XOptions()).ReadAsync(Metrics(), Token);
        result.Outcome.Should().Be(BrokerOutcome.Rejected);
        result.HasPartialErrors.Should().BeTrue();
        result.Observations.Should().AllSatisfy(item => item.Value.Should().BeNull());
        result.Message.Should().NotContain("fake_secret");
    }

    [Fact]
    public async Task Publish_ShouldRequireNumericBindingsAndACheckpointBeforeTelegramSends()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = new TelegramSocialPublishingBroker(client, TelegramOptions());
        var missing = await broker.PublishAsync(new("-100123", "Approved", AccountId: "123"), Token);
        missing.Code.Should().Be("checkpoint_required");
        foreach (var request in new[]
        {
            TelegramRequest() with { AccountId = "456" }, TelegramRequest() with { DestinationId = "@test_channel" },
            TelegramRequest() with { DestinationId = "-100456" }
        })
            ((await broker.PublishAsync(request, Token)).Code).Should().Be("identity_mismatch");
        var failed = await broker.PublishAsync(
            TelegramRequest() with { SaveCheckpointAsync = (_, _) => throw new IOException("fake_secret") },
            Token);
        failed.Outcome.Should().Be(BrokerOutcome.Rejected);
        failed.Code.Should().Be("checkpoint_failed");
        failed.Message.Should().NotContain("fake_secret");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_ShouldCheckpointBeforeSendAndMatchTheReturnedTelegramChannel()
    {
        ProviderReceipt? saved = null;
        using var handler = new FakeHandler(_ =>
        {
            (saved?.Stage).Should().Be("publication_started");
            (saved?.RelatedIds?["account_id"]).Should().Be("123");
            return Json("{\"ok\":true,\"result\":{\"message_id\":71,\"chat\":{\"id\":-100123}}}");
        });
        using var client = new HttpClient(handler);
        var result = await new TelegramSocialPublishingBroker(client, TelegramOptions()).PublishAsync(
            TelegramRequest() with
            {
                SaveCheckpointAsync = (receipt, _) => { saved = receipt; return Task.CompletedTask; }
            },
            Token);
        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        (result.Receipt?.Stage).Should().Be("published");
        (result.Receipt?.RelatedIds?["chat_id"]).Should().Be("-100123");
        (result.Receipt?.RelatedIds?["account_id"]).Should().Be("123");
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
        result.Outcome.Should().Be(BrokerOutcome.Ambiguous);
        result.Code.Should().Be(code);
        (result.Receipt?.Stage).Should().Be("publication_unknown");
        ((await broker.PublishAsync(TelegramRequest() with { ResumeReceipt = result.Receipt }, Token)).Outcome)
            .Should()
            .Be(BrokerOutcome.Unsupported);
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task Publish_ShouldNotSend_WhenXTextDispatchCheckpointFails()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialPublishingBroker(client, XOptions(), new FakeTimeProvider(Now)).PublishAsync(
            XRequest() with { SaveCheckpointAsync = (_, _) => throw new IOException("fake_secret") }, Token);
        result.Outcome.Should().Be(BrokerOutcome.Rejected);
        result.Code.Should().Be("checkpoint_failed");
        handler.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("0")]
    [InlineData("\"3600\"")]
    public async Task Publish_ShouldAvoidFinalPost_WhenXUploadExpiryIsMissingOrInvalid(string expiry)
    {
        using var handler = new FakeHandler(
            _ => Json("{\"data\":{\"id\":\"101\",\"expires_after_secs\":" + expiry + "}}"));
        using var client = new HttpClient(handler);
        var result = await new XSocialPublishingBroker(client, XOptions(), new FakeTimeProvider(Now)).PublishAsync(
            XRequest() with { Media = [new(Image, "image/png")] }, Token);
        result.Outcome.Should().Be(BrokerOutcome.Ambiguous);
        result.Code.Should().Be("upload_expiry_unavailable");
        (result.Receipt?.Stage).Should().Be("media_upload_unknown");
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task Publish_ShouldAvoidFinalPostAndReplay_WhenXUploadHasPartialErrors()
    {
        using var handler = new FakeHandler(
            _ =>
                Json(
                    "{\"data\":{\"id\":\"101\",\"expires_after_secs\":3600},\"errors\":[{\"detail\":\"fake_secr"
                        + "et\"}]}"));
        using var client = new HttpClient(handler);
        var broker = new XSocialPublishingBroker(client, XOptions(), new FakeTimeProvider(Now));
        var input = XRequest() with { Media = [new(Image, "image/png")] };
        var result = await broker.PublishAsync(input, Token);
        result.Outcome.Should().Be(BrokerOutcome.Ambiguous);
        result.Code.Should().Be("partial_upload");
        (result.Receipt?.Stage).Should().Be("media_upload_unknown");
        ((await broker.PublishAsync(input with { ResumeReceipt = result.Receipt }, Token)).Outcome)
            .Should()
            .Be(BrokerOutcome.Unsupported);
        handler.Requests.Should().ContainSingle();
        result.Message.Should().NotContain("fake_secret");
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("missing")]
    [InlineData("wrong_account")]
    [InlineData("wrong_image")]
    public async Task Publish_ShouldRejectWithoutSending_WhenAnUploadResumeIsUnsafe(string failure)
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var related = UploadIds();
        if (failure == "expired") related["expires_at"] = Now.ToString("O", CultureInfo.InvariantCulture);
        if (failure == "missing") related.Remove("expires_at");
        if (failure == "wrong_account") related["account_id"] = "456";
        if (failure == "wrong_image") related["media_sha256"] = "different";
        var result = await new XSocialPublishingBroker(client, XOptions(), new FakeTimeProvider(Now)).PublishAsync(
            XRequest() with
            {
                Media = [new(Image, "image/png")],
                ResumeReceipt = new("x", "media_uploaded", "101", related)
            },
            Token);
        result.Outcome.Should().Be(BrokerOutcome.Rejected);
        result.Code.Should().Be("upload_unverified_or_expired");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_ShouldResumeWithoutASecondUpload_WhenOwnedMediaIsUnexpired()
    {
        var stages = new List<string>();
        using var handler = new FakeHandler(_ =>
        {
            stages.Should().Equal(new[] { "publication_started" });
            return Json("{\"data\":{\"id\":\"201\"}}");
        });
        using var client = new HttpClient(handler);
        var result = await new XSocialPublishingBroker(client, XOptions(), new FakeTimeProvider(Now)).PublishAsync(
            XRequest() with
            {
                Media = [new(Image, "image/png")], ResumeReceipt = new("x", "media_uploaded", "101", UploadIds()),
                SaveCheckpointAsync = (receipt, _) => { stages.Add(receipt.Stage); return Task.CompletedTask; }
            }, Token);
        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        (result.Receipt?.RelatedIds?["account_id"]).Should().Be("123");
        handler.Requests.Should().ContainSingle().Which.Uri.Should().EndWith("/2/tweets");
    }

    [Theory]
    [InlineData("{\"data\":{\"id\":\"201\"},\"errors\":[{\"detail\":\"fake_secret\"}]}")]
    [InlineData("{\"data\":{\"id\":\"201\",\"author_id\":\"456\"}}")]
    public async Task Publish_ShouldNotClaimSuccess_WhenXFinalReceiptHasConflictingEvidence(string response)
    {
        using var handler = new FakeHandler(_ => Json(response));
        using var client = new HttpClient(handler);
        var result = await new XSocialPublishingBroker(
            client,
            XOptions(),
            new FakeTimeProvider(Now)).PublishAsync(XRequest(), Token);
        result.Outcome.Should().Be(BrokerOutcome.Ambiguous);
        (result.Receipt?.Stage).Should().Be("publication_unknown");
        (result.Receipt?.RelatedIds?["account_id"]).Should().Be("123");
        handler.Requests.Should().ContainSingle();
        result.Message.Should().NotContain("fake_secret");
    }

    [Fact]
    public async Task Publish_ShouldBindATextOnlyXReceiptToTheAccount_WhenDispatchIsDurable()
    {
        ProviderReceipt? saved = null;
        using var handler = new FakeHandler(_ =>
        {
            (saved?.Stage).Should().Be("publication_started");
            return Json("{\"data\":{\"id\":\"201\"}}");
        });
        using var client = new HttpClient(handler);
        var result = await new XSocialPublishingBroker(client, XOptions(), new FakeTimeProvider(Now)).PublishAsync(
            XRequest() with { SaveCheckpointAsync = (receipt, _) => { saved = receipt; return Task.CompletedTask; } },
            Token);
        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        (result.Receipt?.ExternalId).Should().Be("201");
        (result.Receipt?.RelatedIds?["account_id"]).Should().Be("123");
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
                : "{\"ok\":true,\"result\":{\"id\":-100123,\"type\":\"channel\",\"title\":\"Test "
                    + "channel\",\"username\":\"test_channel\"}}");
        return Json(failure == "permission"
            ? "{\"ok\":true,\"result\":{\"status\":\"administrator\",\"can_post_messages\":false,\"user\":{\"id\""
                + ":123,\"is_bot\":true}}}"
            : failure == "member"
                ? "{\"ok\":true,\"result\":{\"status\":\"administrator\",\"can_post_messages\":true,\"user\":{\"i"
                    + "d\":456,\"is_bot\":true}}}"
                : "{\"ok\":true,\"result\":{\"status\":\"administrator\",\"can_post_messages\":true,\"user\":{\"i"
                    + "d\":123,\"is_bot\":true}}}");
    });

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private sealed record CapturedRequest(string Method, string Uri, string Body, string? Authorization);
    private sealed class FakeHandler(Func<CapturedRequest, HttpResponseMessage> response) : HttpMessageHandler
    {
        internal List<CapturedRequest> Requests { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var captured = new CapturedRequest(request.Method.Method, request.RequestUri!.ToString(),
                request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken),
                request.Headers.Authorization?.ToString());
            Requests.Add(captured);
            return response(captured);
        }
    }
}
