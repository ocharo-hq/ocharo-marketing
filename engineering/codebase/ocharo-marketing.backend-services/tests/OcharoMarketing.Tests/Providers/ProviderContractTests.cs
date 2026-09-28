using System.Net;
using System.Text;
using System.Text.Json;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Infrastructure.Providers;
using OcharoMarketing.Infrastructure.Providers.Brokers;
using Xunit;

namespace OcharoMarketing.Tests.Providers;

public sealed class ProviderContractTests
{
    private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j9t8AAAAASUVORK5CYII=";
    private static byte[] Image => Convert.FromBase64String(Png);
    private static readonly CancellationToken Token = CancellationToken.None;

    [Fact]
    public async Task Publish_ShouldRejectWithoutSending_WhenExecutionIsDisabled()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        ISocialPublishingBroker[] brokers =
        [
            new TelegramSocialPublishingBroker(client, new() { BotToken = "123:fake_secret" }),
            new XSocialPublishingBroker(client, new() { AccessToken = "fake_secret", UserId = "123" }),
            new InstagramSocialPublishingBroker(client, new() { AccessToken = "fake_secret", GraphVersion = "v24.0" }),
            new WhatsAppSocialPublishingBroker(client, new() { AccessToken = "fake_secret", PhoneNumberId = "123", GraphVersion = "v24.0" })
        ];
        foreach (var broker in brokers)
            Assert.Equal(BrokerOutcome.Rejected, (await broker.PublishAsync(new("123", "Approved"), Token)).Outcome);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Generate_ShouldSendCurrentJsonContract_WhenOpenAiGenerationOrEditIsRequested(bool edit)
    {
        using var handler = new FakeHandler(_ => Json(JsonSerializer.Serialize(new { data = new[] { new { b64_json = Png } } })));
        using var client = new HttpClient(handler);
        var broker = new OpenAiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });
        var input = new ImageGenerationRequest("Preserve garment", edit ? [new(Image, "image/png")] : null);

        var result = await broker.GenerateAsync(input, Token);

        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal(Image, Assert.Single(result.Images).Data);
        var sent = Assert.Single(handler.Requests);
        Assert.EndsWith(edit ? "/images/edits" : "/images/generations", sent.Uri);
        Assert.Equal("Bearer fake_secret", sent.Authorization);
        using var body = JsonDocument.Parse(sent.Body);
        Assert.Equal("gpt-image-2.5-flare", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("1024x1280", body.RootElement.GetProperty("size").GetString());
        if (edit)
            Assert.StartsWith("data:image/png;base64,", body.RootElement.GetProperty("images")[0].GetProperty("image_url").GetString());
        else Assert.Equal(1, body.RootElement.GetProperty("n").GetInt32());
        Assert.DoesNotContain("fake_secret", sent.Body);
    }

    [Fact]
    public async Task Generate_ShouldRejectBeforeSending_WhenCardDimensionsViolateImageSizeGrid()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = new OpenAiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });

        var result = await broker.GenerateAsync(new("Garment scene", Size: "1080x1350"), Token);

        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Generate_ShouldReturnAmbiguousWithoutRetry_WhenTransportFailsAfterDispatch()
    {
        using var handler = new FakeHandler(_ => throw new HttpRequestException("fake_secret must never escape"));
        using var client = new HttpClient(handler);
        var broker = new OpenAiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });

        var result = await broker.GenerateAsync(new("Garment scene"), Token);

        Assert.Equal(BrokerOutcome.Ambiguous, result.Outcome);
        Assert.Single(handler.Requests);
        Assert.DoesNotContain("fake_secret", result.Message);
    }

    [Fact]
    public async Task Generate_ShouldRejectFalseArtifacts_WhenProviderReturnsNonImageBytes()
    {
        using var handler = new FakeHandler(_ => Json("{\"data\":[{\"b64_json\":\"AQID\"}]}"));
        using var client = new HttpClient(handler);
        var broker = new OpenAiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });

        var result = await broker.GenerateAsync(new("Garment scene"), Token);

        Assert.Equal(BrokerOutcome.Ambiguous, result.Outcome);
        Assert.Empty(result.Images);
    }

    [Fact]
    public async Task Assist_ShouldParseSuggestedTextAndVisionInput_WhenResponsesReturnsOutput()
    {
        using var handler = new FakeHandler(_ => Json(
            "{\"id\":\"resp_123\",\"output\":[{\"type\":\"message\",\"content\":[{\"type\":\"output_text\",\"text\":\"Suggested copy\"}]}]}"));
        using var client = new HttpClient(handler);
        var broker = new OpenAiContentAssistanceBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });

        var result = await broker.AssistAsync(new("Describe visible garment", "vision", "uz", [new(Image, "image/png")]), Token);

        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal("Suggested copy", result.Text);
        Assert.Equal("resp_123", result.Receipt?.ExternalId);
        using var body = JsonDocument.Parse(Assert.Single(handler.Requests).Body);
        Assert.Equal("input_image", body.RootElement.GetProperty("input")[0].GetProperty("content")[1].GetProperty("type").GetString());
    }

    [Fact]
    public async Task Generate_ShouldCheckpointInteractionAndReadImage_WhenGeminiReturnsTypedOutput()
    {
        using var handler = new FakeHandler(_ => Json(JsonSerializer.Serialize(new
        {
            id = "interaction_123", status = "completed",
            steps = new[] { new { type = "model_output", content = new[] { new { type = "image", mime_type = "image/png", data = Png } } } }
        })));
        using var client = new HttpClient(handler);
        var broker = new GeminiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });
        ProviderReceipt? checkpoint = null;

        var result = await broker.GenerateAsync(new("Preserve garment", [new(Image, "image/png")],
            SaveCheckpointAsync: (receipt, _) => { checkpoint = receipt; return Task.CompletedTask; }), Token);

        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal("interaction_123", checkpoint?.ExternalId);
        Assert.Equal(Image, Assert.Single(result.Images).Data);
        var sent = Assert.Single(handler.Requests);
        Assert.Equal("fake_secret", sent.GoogleKey);
        using var body = JsonDocument.Parse(sent.Body);
        Assert.Equal("image", body.RootElement.GetProperty("input")[0].GetProperty("type").GetString());
        Assert.Equal("4:5", body.RootElement.GetProperty("response_format").GetProperty("aspect_ratio").GetString());
        Assert.DoesNotContain("fake_secret", sent.Body);
    }

    [Fact]
    public async Task Generate_ShouldExcludeUserInputsAndThoughtImages_WhenGeminiHasNoFinalOutput()
    {
        using var handler = new FakeHandler(_ => Json(JsonSerializer.Serialize(new
        {
            id = "interaction_123", status = "completed",
            steps = new[]
            {
                new { type = "user_input", content = new[] { new { type = "image", mime_type = "image/png", data = Png } } },
                new { type = "thought", content = new[] { new { type = "image", mime_type = "image/png", data = Png } } }
            }
        })));
        using var client = new HttpClient(handler);
        var broker = new GeminiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });

        var result = await broker.GenerateAsync(new("Preserve garment"), Token);

        Assert.Equal(BrokerOutcome.Ambiguous, result.Outcome);
        Assert.Empty(result.Images);
    }

    [Fact]
    public async Task Generate_ShouldReturnUnsupportedWithoutSending_WhenGeminiTransparencyIsRequested()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = new GeminiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });

        var result = await broker.GenerateAsync(new("Preserve garment", TransparentBackground: true), Token);

        Assert.Equal(BrokerOutcome.Unsupported, result.Outcome);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Publish_ShouldUploadPhotoAndExtractMessageReceipt_WhenTelegramAccepts()
    {
        using var handler = new FakeHandler(_ => Json("{\"ok\":true,\"result\":{\"message_id\":71,\"chat\":{\"id\":-100123}}}"));
        using var client = new HttpClient(handler);
        var broker = new TelegramSocialPublishingBroker(client, new() { Enabled = true, BotToken = "123:fake_secret", BotId = "123", DestinationChatId = "-100123" });

        var result = await broker.PublishAsync(new("-100123", "Approved caption", [new(Image, "image/png")], SaveCheckpointAsync: (_, _) => Task.CompletedTask, AccountId: "123"), Token);

        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal("71", result.Receipt?.ExternalId);
        Assert.Equal("-100123", result.Receipt?.RelatedIds?["chat_id"]);
        var sent = Assert.Single(handler.Requests);
        Assert.Contains("/sendPhoto", sent.Uri);
        Assert.Contains("multipart/form-data", sent.ContentType);
        Assert.Contains("Approved caption", sent.Body);
        Assert.DoesNotContain("fake_secret", result.Message);
    }

    [Theory]
    [InlineData(400, BrokerOutcome.Rejected)]
    [InlineData(500, BrokerOutcome.Ambiguous)]
    public async Task Publish_ShouldPreserveUnknownOutcomeAndRedactErrors_WhenTelegramReturnsFailure(int status, BrokerOutcome expected)
    {
        using var handler = new FakeHandler(_ => Json("{\"description\":\"fake_secret\"}", (HttpStatusCode)status));
        using var client = new HttpClient(handler);
        var broker = new TelegramSocialPublishingBroker(client, new() { Enabled = true, BotToken = "123:fake_secret", BotId = "123", DestinationChatId = "-100123" });

        var result = await broker.PublishAsync(new("-100123", "Approved", SaveCheckpointAsync: (_, _) => Task.CompletedTask, AccountId: "123"), Token);

        Assert.Equal(expected, result.Outcome);
        Assert.Single(handler.Requests);
        Assert.DoesNotContain("fake_secret", result.Message);
    }

    [Fact]
    public async Task Publish_ShouldPersistUploadBeforePublication_WhenXImageIsSelected()
    {
        var persisted = false;
        using var handler = new FakeHandler(sent =>
        {
            if (sent.Uri.EndsWith("/media/upload")) return Json("{\"data\":{\"id\":\"101\",\"expires_after_secs\":3600}}");
            Assert.True(persisted);
            return Json("{\"data\":{\"id\":\"201\"}}");
        });
        using var client = new HttpClient(handler);
        var broker = new XSocialPublishingBroker(client, new() { Enabled = true, AccessToken = "fake_secret", UserId = "123" });

        var result = await broker.PublishAsync(new("123", "Approved", [new(Image, "image/png")],
            SaveCheckpointAsync: (receipt, _) => { if (receipt.Stage == "media_uploaded") { Assert.Equal("101", receipt.ExternalId); persisted = true; } return Task.CompletedTask; }), Token);

        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal("201", result.Receipt?.ExternalId);
        Assert.Equal(2, handler.Requests.Count);
        using var body = JsonDocument.Parse(handler.Requests[1].Body);
        Assert.Equal("101", body.RootElement.GetProperty("media").GetProperty("media_ids")[0].GetString());
    }

    [Fact]
    public async Task Publish_ShouldAvoidFinalSideEffect_WhenUploadCheckpointFails()
    {
        using var handler = new FakeHandler(_ => Json("{\"data\":{\"id\":\"101\",\"expires_after_secs\":3600}}"));
        using var client = new HttpClient(handler);
        var broker = new XSocialPublishingBroker(client, new() { Enabled = true, AccessToken = "fake_secret", UserId = "123" });

        var result = await broker.PublishAsync(new("123", "Approved", [new(Image, "image/png")],
            SaveCheckpointAsync: (receipt, _) => receipt.Stage == "media_uploaded" ? throw new IOException("Storage unavailable") : Task.CompletedTask), Token);

        Assert.Equal(BrokerOutcome.Ambiguous, result.Outcome);
        Assert.Single(handler.Requests);
        Assert.Equal("101", result.Receipt?.ExternalId);
    }

    [Fact]
    public async Task Publish_ShouldBlockWrongAccount_WhenXDestinationDiffers()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = new XSocialPublishingBroker(client, new() { Enabled = true, AccessToken = "fake_secret", UserId = "123" });

        var result = await broker.PublishAsync(new("456", "Approved"), Token);

        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Publish_ShouldSaveContainerAndPublishOnlyWhenFinished_WhenInstagramImageIsReady()
    {
        var persisted = false;
        using var handler = new FakeHandler(sent =>
        {
            if (sent.Uri.EndsWith("/123/media")) return Json("{\"id\":\"container_123\"}");
            Assert.True(persisted);
            return sent.Method == "GET" ? Json("{\"status_code\":\"FINISHED\"}") : Json("{\"id\":\"ig_media_123\"}");
        });
        using var client = new HttpClient(handler);
        var broker = new InstagramSocialPublishingBroker(client,
            new() { Enabled = true, AccessToken = "fake_secret", GraphVersion = "v24.0" });

        var result = await broker.PublishAsync(new("123", "Approved", [new([], "image/jpeg", PublicUrl: "https://media.example/card.jpg")],
            SaveCheckpointAsync: (receipt, _) => { Assert.Equal("container_123", receipt.ExternalId); persisted = true; return Task.CompletedTask; }), Token);

        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal("ig_media_123", result.Receipt?.ExternalId);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Contains("graph.instagram.com", handler.Requests[0].Uri);
        Assert.EndsWith("/123/media_publish", handler.Requests[2].Uri);
    }

    [Fact]
    public async Task Publish_ShouldReturnPendingWithoutPublishing_WhenInstagramContainerIsProcessing()
    {
        using var handler = new FakeHandler(sent => sent.Method == "GET"
            ? Json("{\"status_code\":\"IN_PROGRESS\"}") : Json("{\"id\":\"container_123\"}"));
        using var client = new HttpClient(handler);
        var broker = new InstagramSocialPublishingBroker(client,
            new() { Enabled = true, AccessToken = "fake_secret", GraphVersion = "v24.0" });

        var result = await broker.PublishAsync(new("123", "Approved", [new([], "image/jpeg", PublicUrl: "https://media.example/card.jpg")],
            SaveCheckpointAsync: (_, _) => Task.CompletedTask), Token);

        Assert.Equal(BrokerOutcome.Pending, result.Outcome);
        Assert.Equal("container_created", result.Receipt?.Stage);
        Assert.Equal(2, handler.Requests.Count);
        Assert.DoesNotContain(handler.Requests, sent => sent.Uri.EndsWith("/media_publish"));
    }

    [Fact]
    public async Task Publish_ShouldBlockReplayOfUnknownReceipt_WhenInstagramFinalResponseIsLost()
    {
        var stages = new List<string>();
        using var handler = new FakeHandler(sent => sent.Method == "GET" ? Json("{\"status_code\":\"FINISHED\"}") :
            sent.Uri.EndsWith("/media_publish") ? Json("{}", HttpStatusCode.InternalServerError) : Json("{\"id\":\"container_123\"}"));
        using var client = new HttpClient(handler);
        var broker = new InstagramSocialPublishingBroker(client,
            new() { Enabled = true, AccessToken = "fake_secret", GraphVersion = "v24.0" });
        var input = new SocialPublishRequest("123", "Approved", [new([], "image/jpeg", PublicUrl: "https://media.example/card.jpg")],
            SaveCheckpointAsync: (receipt, _) => { stages.Add(receipt.Stage); return Task.CompletedTask; });

        var unknown = await broker.PublishAsync(input, Token);
        var replay = await broker.PublishAsync(input with { ResumeReceipt = unknown.Receipt }, Token);

        Assert.Equal(BrokerOutcome.Ambiguous, unknown.Outcome);
        Assert.Equal("publication_unknown", unknown.Receipt?.Stage);
        Assert.Equal(BrokerOutcome.Unsupported, replay.Outcome);
        Assert.Equal(new[] { "container_created", "publication_started" }, stages);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Theory]
    [InlineData("image/png", "https://media.example/card.png")]
    [InlineData("image/jpeg", "https://127.0.0.1/card.jpg")]
    [InlineData("image/jpeg", "http://media.example/card.jpg")]
    public async Task Publish_ShouldRejectUnsupportedMediaWithoutSending_WhenInstagramPhotoIsNotPublicJpeg(string mime, string url)
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = new InstagramSocialPublishingBroker(client,
            new() { Enabled = true, AccessToken = "fake_secret", GraphVersion = "v24.0" });

        var result = await broker.PublishAsync(new("123", "Approved", [new([], mime, PublicUrl: url)],
            SaveCheckpointAsync: (_, _) => Task.CompletedTask), Token);

        Assert.Equal(BrokerOutcome.Unsupported, result.Outcome);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Publish_ShouldRequirePermissionEvenForTemplate_WhenWhatsAppRecipientConsentIsMissing()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = WhatsApp(client);

        var result = await broker.PublishAsync(new("998901234567", "Approved", Template: new("approved_template", "en")), Token);

        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Equal("permission_required", result.Code);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Publish_ShouldRequireTemplate_WhenWhatsAppServiceWindowIsUnconfirmed()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = WhatsApp(client);

        var result = await broker.PublishAsync(new("998901234567", "Approved", RecipientPermissionConfirmed: true), Token);

        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Equal("template_required", result.Code);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Publish_ShouldReturnAcceptanceWithoutClaimingDelivery_WhenWhatsAppTemplateIsAccepted()
    {
        using var handler = new FakeHandler(_ => Json("{\"messages\":[{\"id\":\"wamid.123\"}]}"));
        using var client = new HttpClient(handler);
        var broker = WhatsApp(client);

        var result = await broker.PublishAsync(new("998901234567", "",
            Template: new("approved_template", "en"), RecipientPermissionConfirmed: true), Token);

        Assert.Equal(BrokerOutcome.Succeeded, result.Outcome);
        Assert.Equal("accepted", result.Receipt?.Stage);
        Assert.Contains("delivery remains unverified", result.Message);
        using var body = JsonDocument.Parse(Assert.Single(handler.Requests).Body);
        Assert.Equal("template", body.RootElement.GetProperty("type").GetString());
        Assert.Equal("approved_template", body.RootElement.GetProperty("template").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Publish_ShouldAvoidDispatch_WhenCancellationIsAlreadyRequested()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = new TelegramSocialPublishingBroker(client, new() { Enabled = true, BotToken = "123:fake_secret", BotId = "123", DestinationChatId = "-100123" });
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        var result = await broker.PublishAsync(new("-100123", "Approved", SaveCheckpointAsync: (_, _) => Task.CompletedTask, AccountId: "123"), cancelled.Token);

        Assert.Equal(BrokerOutcome.Rejected, result.Outcome);
        Assert.Empty(handler.Requests);
    }

    private static WhatsAppSocialPublishingBroker WhatsApp(HttpClient client)
        => new(client, new() { Enabled = true, AccessToken = "fake_secret", PhoneNumberId = "123", GraphVersion = "v24.0" });

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed record CapturedRequest(string Method, string Uri, string Body, string? ContentType, string? Authorization, string? GoogleKey);

    private sealed class FakeHandler(Func<CapturedRequest, HttpResponseMessage> response) : HttpMessageHandler
    {
        internal List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var captured = new CapturedRequest(request.Method.Method, request.RequestUri!.ToString(),
                request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken),
                request.Content?.Headers.ContentType?.ToString(), request.Headers.Authorization?.ToString(),
                request.Headers.TryGetValues("x-goog-api-key", out var keys) ? keys.Single() : null);
            Requests.Add(captured);
            return response(captured);
        }
    }
}
