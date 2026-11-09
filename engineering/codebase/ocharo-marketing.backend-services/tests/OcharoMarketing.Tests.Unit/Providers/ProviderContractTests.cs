using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Infrastructure.Providers;
using OcharoMarketing.Infrastructure.Providers.Brokers;
using Xunit;

namespace OcharoMarketing.Tests.Unit.Providers;

public sealed class ProviderContractTests
{
    private const string Png =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j9t8AAAAASUVORK5CYII=";
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
            new WhatsAppSocialPublishingBroker(
                client,
                new() { AccessToken = "fake_secret", PhoneNumberId = "123", GraphVersion = "v24.0" })
        ];
        foreach (var broker in brokers)
            ((await broker.PublishAsync(new("123", "Approved"), Token)).Outcome).Should().Be(BrokerOutcome.Rejected);
        handler.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Generate_ShouldSendCurrentJsonContract_WhenOpenAiGenerationOrEditIsRequested(bool edit)
    {
        using var handler = new FakeHandler(
            _ => Json(JsonSerializer.Serialize(new { data = new[] { new { b64_json = Png } } })));
        using var client = new HttpClient(handler);
        var broker = new OpenAiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });
        var input = new ImageGenerationRequest("Preserve garment", edit ? [new(Image, "image/png")] : null);

        var result = await broker.GenerateAsync(input, Token);

        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        result.Images.Should().ContainSingle().Which.Data.Should().Equal(Image);
        var sent = handler.Requests.Should().ContainSingle().Which;
        sent.Uri.Should().EndWith(edit ? "/images/edits" : "/images/generations");
        sent.Authorization.Should().Be("Bearer fake_secret");
        using var body = JsonDocument.Parse(sent.Body);
        (body.RootElement.GetProperty("model").GetString()).Should().Be("gpt-image-2.5-flare");
        (body.RootElement.GetProperty("size").GetString()).Should().Be("1024x1280");
        if (edit)
            (body.RootElement.GetProperty("images")[0].GetProperty("image_url").GetString())
                .Should()
                .StartWith("data:image/png;base64,");
        else (body.RootElement.GetProperty("n").GetInt32()).Should().Be(1);
        sent.Body.Should().NotContain("fake_secret");
    }

    [Fact]
    public async Task Generate_ShouldRejectBeforeSending_WhenCardDimensionsViolateImageSizeGrid()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = new OpenAiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });

        var result = await broker.GenerateAsync(new("Garment scene", Size: "1080x1350"), Token);

        result.Outcome.Should().Be(BrokerOutcome.Rejected);
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Generate_ShouldReturnAmbiguousWithoutRetry_WhenTransportFailsAfterDispatch()
    {
        using var handler = new FakeHandler(_ => throw new HttpRequestException("fake_secret must never escape"));
        using var client = new HttpClient(handler);
        var broker = new OpenAiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });

        var result = await broker.GenerateAsync(new("Garment scene"), Token);

        result.Outcome.Should().Be(BrokerOutcome.Ambiguous);
        handler.Requests.Should().ContainSingle();
        result.Message.Should().NotContain("fake_secret");
    }

    [Fact]
    public async Task Generate_ShouldRejectFalseArtifacts_WhenProviderReturnsNonImageBytes()
    {
        using var handler = new FakeHandler(_ => Json("{\"data\":[{\"b64_json\":\"AQID\"}]}"));
        using var client = new HttpClient(handler);
        var broker = new OpenAiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });

        var result = await broker.GenerateAsync(new("Garment scene"), Token);

        result.Outcome.Should().Be(BrokerOutcome.Ambiguous);
        result.Images.Should().BeEmpty();
    }

    [Fact]
    public async Task Assist_ShouldParseSuggestedTextAndVisionInput_WhenResponsesReturnsOutput()
    {
        using var handler = new FakeHandler(_ => Json(
            "{\"id\":\"resp_123\",\"output\":[{\"type\":\"message\",\"content\":[{\"type\":\"output_text\",\"te"
                + "xt\":\"Suggested copy\"}]}]}"));
        using var client = new HttpClient(handler);
        var broker = new OpenAiContentAssistanceBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });

        var result = await broker.AssistAsync(
            new("Describe visible garment", "vision", "uz", [new(Image, "image/png")]),
            Token);

        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        result.Text.Should().Be("Suggested copy");
        (result.Receipt?.ExternalId).Should().Be("resp_123");
        using var body = JsonDocument.Parse(handler.Requests.Should().ContainSingle().Which.Body);
        (body.RootElement.GetProperty("input")[0].GetProperty("content")[1].GetProperty("type").GetString())
            .Should()
            .Be("input_image");
    }

    [Fact]
    public async Task Generate_ShouldCheckpointInteractionAndReadImage_WhenGeminiReturnsTypedOutput()
    {
        using var handler = new FakeHandler(_ => Json(JsonSerializer.Serialize(new
        {
            id = "interaction_123", status = "completed",
            steps = new[]
            {
                new
                {
                    type = "model_output",
                    content = new[] { new { type = "image", mime_type = "image/png", data = Png } }
                }
            }
        })));
        using var client = new HttpClient(handler);
        var broker = new GeminiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });
        ProviderReceipt? checkpoint = null;

        var result = await broker.GenerateAsync(new("Preserve garment", [new(Image, "image/png")],
            SaveCheckpointAsync: (receipt, _) => { checkpoint = receipt; return Task.CompletedTask; }), Token);

        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        (checkpoint?.ExternalId).Should().Be("interaction_123");
        result.Images.Should().ContainSingle().Which.Data.Should().Equal(Image);
        var sent = handler.Requests.Should().ContainSingle().Which;
        sent.GoogleKey.Should().Be("fake_secret");
        using var body = JsonDocument.Parse(sent.Body);
        (body.RootElement.GetProperty("input")[0].GetProperty("type").GetString()).Should().Be("image");
        (body.RootElement.GetProperty("response_format").GetProperty("aspect_ratio").GetString()).Should().Be("4:5");
        sent.Body.Should().NotContain("fake_secret");
    }

    [Fact]
    public async Task Generate_ShouldExcludeUserInputsAndThoughtImages_WhenGeminiHasNoFinalOutput()
    {
        using var handler = new FakeHandler(_ => Json(JsonSerializer.Serialize(new
        {
            id = "interaction_123", status = "completed",
            steps = new[]
            {
                new
                {
                    type = "user_input",
                    content = new[] { new { type = "image", mime_type = "image/png", data = Png } }
                },
                new
                {
                    type = "thought",
                    content = new[] { new { type = "image", mime_type = "image/png", data = Png } }
                }
            }
        })));
        using var client = new HttpClient(handler);
        var broker = new GeminiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });

        var result = await broker.GenerateAsync(new("Preserve garment"), Token);

        result.Outcome.Should().Be(BrokerOutcome.Ambiguous);
        result.Images.Should().BeEmpty();
    }

    [Fact]
    public async Task Generate_ShouldReturnUnsupportedWithoutSending_WhenGeminiTransparencyIsRequested()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = new GeminiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });

        var result = await broker.GenerateAsync(new("Preserve garment", TransparentBackground: true), Token);

        result.Outcome.Should().Be(BrokerOutcome.Unsupported);
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_ShouldUploadPhotoAndExtractMessageReceipt_WhenTelegramAccepts()
    {
        using var handler = new FakeHandler(
            _ => Json("{\"ok\":true,\"result\":{\"message_id\":71,\"chat\":{\"id\":-100123}}}"));
        using var client = new HttpClient(handler);
        var broker = new TelegramSocialPublishingBroker(
            client,
            new() { Enabled = true, BotToken = "123:fake_secret", BotId = "123", DestinationChatId = "-100123" });

        var result = await broker.PublishAsync(
            new(
                "-100123",
                "Approved caption",
                [new(Image, "image/png")],
                SaveCheckpointAsync: (_, _) => Task.CompletedTask,
                AccountId: "123"),
            Token);

        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        (result.Receipt?.ExternalId).Should().Be("71");
        (result.Receipt?.RelatedIds?["chat_id"]).Should().Be("-100123");
        var sent = handler.Requests.Should().ContainSingle().Which;
        sent.Uri.Should().Contain("/sendPhoto");
        sent.ContentType.Should().Contain("multipart/form-data");
        sent.Body.Should().Contain("Approved caption");
        result.Message.Should().NotContain("fake_secret");
    }

    [Theory]
    [InlineData(400, BrokerOutcome.Rejected)]
    [InlineData(500, BrokerOutcome.Ambiguous)]
    public async Task Publish_ShouldPreserveUnknownOutcomeAndRedactErrors_WhenTelegramReturnsFailure(
        int status,
        BrokerOutcome expected)
    {
        using var handler = new FakeHandler(_ => Json("{\"description\":\"fake_secret\"}", (HttpStatusCode)status));
        using var client = new HttpClient(handler);
        var broker = new TelegramSocialPublishingBroker(
            client,
            new() { Enabled = true, BotToken = "123:fake_secret", BotId = "123", DestinationChatId = "-100123" });

        var result = await broker.PublishAsync(
            new("-100123", "Approved", SaveCheckpointAsync: (_, _) => Task.CompletedTask, AccountId: "123"),
            Token);

        result.Outcome.Should().Be(expected);
        handler.Requests.Should().ContainSingle();
        result.Message.Should().NotContain("fake_secret");
    }

    [Fact]
    public async Task Publish_ShouldPersistUploadBeforePublication_WhenXImageIsSelected()
    {
        var persisted = false;
        using var handler = new FakeHandler(sent =>
        {
            if (sent.Uri.EndsWith("/media/upload"))
                return Json("{\"data\":{\"id\":\"101\",\"expires_after_secs\":3600}}");
            persisted.Should().BeTrue();
            return Json("{\"data\":{\"id\":\"201\"}}");
        });
        using var client = new HttpClient(handler);
        var broker = new XSocialPublishingBroker(
            client,
            new() { Enabled = true, AccessToken = "fake_secret", UserId = "123" });

        var result = await broker.PublishAsync(new("123", "Approved", [new(Image, "image/png")],
            SaveCheckpointAsync: (receipt, _) =>
            {
                if (receipt.Stage == "media_uploaded")
                {
                    receipt.ExternalId.Should().Be("101");
                    persisted = true;
                }
                return Task.CompletedTask;
            }),
            Token);

        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        (result.Receipt?.ExternalId).Should().Be("201");
        handler.Requests.Count.Should().Be(2);
        using var body = JsonDocument.Parse(handler.Requests[1].Body);
        (body.RootElement.GetProperty("media").GetProperty("media_ids")[0].GetString()).Should().Be("101");
    }

    [Fact]
    public async Task Publish_ShouldAvoidFinalSideEffect_WhenUploadCheckpointFails()
    {
        using var handler = new FakeHandler(_ => Json("{\"data\":{\"id\":\"101\",\"expires_after_secs\":3600}}"));
        using var client = new HttpClient(handler);
        var broker = new XSocialPublishingBroker(
            client,
            new() { Enabled = true, AccessToken = "fake_secret", UserId = "123" });

        var result = await broker.PublishAsync(new("123", "Approved", [new(Image, "image/png")],
            SaveCheckpointAsync: (receipt, _) =>
                receipt.Stage == "media_uploaded" ? throw new IOException("Storage unavailable") : Task.CompletedTask),
            Token);

        result.Outcome.Should().Be(BrokerOutcome.Ambiguous);
        handler.Requests.Should().ContainSingle();
        (result.Receipt?.ExternalId).Should().Be("101");
    }

    [Fact]
    public async Task Publish_ShouldBlockWrongAccount_WhenXDestinationDiffers()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = new XSocialPublishingBroker(
            client,
            new() { Enabled = true, AccessToken = "fake_secret", UserId = "123" });

        var result = await broker.PublishAsync(new("456", "Approved"), Token);

        result.Outcome.Should().Be(BrokerOutcome.Rejected);
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_ShouldSaveContainerAndPublishOnlyWhenFinished_WhenInstagramImageIsReady()
    {
        var persisted = false;
        using var handler = new FakeHandler(sent =>
        {
            if (sent.Uri.EndsWith("/123/media")) return Json("{\"id\":\"container_123\"}");
            persisted.Should().BeTrue();
            return sent.Method == "GET" ? Json("{\"status_code\":\"FINISHED\"}") : Json("{\"id\":\"ig_media_123\"}");
        });
        using var client = new HttpClient(handler);
        var broker = new InstagramSocialPublishingBroker(client,
            new() { Enabled = true, AccessToken = "fake_secret", GraphVersion = "v24.0" });

        var result = await broker.PublishAsync(
            new("123", "Approved", [new([], "image/jpeg", PublicUrl: "https://media.example/card.jpg")],
            SaveCheckpointAsync: (receipt, _) =>
            {
                receipt.ExternalId.Should().Be("container_123");
                persisted = true;
                return Task.CompletedTask;
            }),
            Token);

        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        (result.Receipt?.ExternalId).Should().Be("ig_media_123");
        handler.Requests.Count.Should().Be(3);
        handler.Requests[0].Uri.Should().Contain("graph.instagram.com");
        handler.Requests[2].Uri.Should().EndWith("/123/media_publish");
    }

    [Fact]
    public async Task Publish_ShouldReturnPendingWithoutPublishing_WhenInstagramContainerIsProcessing()
    {
        using var handler = new FakeHandler(sent => sent.Method == "GET"
            ? Json("{\"status_code\":\"IN_PROGRESS\"}") : Json("{\"id\":\"container_123\"}"));
        using var client = new HttpClient(handler);
        var broker = new InstagramSocialPublishingBroker(client,
            new() { Enabled = true, AccessToken = "fake_secret", GraphVersion = "v24.0" });

        var result = await broker.PublishAsync(
            new("123", "Approved", [new([], "image/jpeg", PublicUrl: "https://media.example/card.jpg")],
            SaveCheckpointAsync: (_, _) => Task.CompletedTask), Token);

        result.Outcome.Should().Be(BrokerOutcome.Pending);
        (result.Receipt?.Stage).Should().Be("container_created");
        handler.Requests.Count.Should().Be(2);
        handler.Requests.Should().NotContain(sent => sent.Uri.EndsWith("/media_publish"));
    }

    [Fact]
    public async Task Publish_ShouldBlockReplayOfUnknownReceipt_WhenInstagramFinalResponseIsLost()
    {
        var stages = new List<string>();
        using var handler = new FakeHandler(sent => sent.Method == "GET" ? Json("{\"status_code\":\"FINISHED\"}") :
            sent.Uri.EndsWith("/media_publish")
                ? Json("{}", HttpStatusCode.InternalServerError)
                : Json("{\"id\":\"container_123\"}"));
        using var client = new HttpClient(handler);
        var broker = new InstagramSocialPublishingBroker(client,
            new() { Enabled = true, AccessToken = "fake_secret", GraphVersion = "v24.0" });
        var input = new SocialPublishRequest(
            "123",
            "Approved",
            [new([], "image/jpeg", PublicUrl: "https://media.example/card.jpg")],
            SaveCheckpointAsync: (receipt, _) => { stages.Add(receipt.Stage); return Task.CompletedTask; });

        var unknown = await broker.PublishAsync(input, Token);
        var replay = await broker.PublishAsync(input with { ResumeReceipt = unknown.Receipt }, Token);

        unknown.Outcome.Should().Be(BrokerOutcome.Ambiguous);
        (unknown.Receipt?.Stage).Should().Be("publication_unknown");
        replay.Outcome.Should().Be(BrokerOutcome.Unsupported);
        stages.Should().Equal(new[] { "container_created", "publication_started" });
        handler.Requests.Count.Should().Be(3);
    }

    [Theory]
    [InlineData("image/png", "https://media.example/card.png")]
    [InlineData("image/jpeg", "https://127.0.0.1/card.jpg")]
    [InlineData("image/jpeg", "http://media.example/card.jpg")]
    public async Task Publish_ShouldRejectUnsupportedMediaWithoutSending_WhenInstagramPhotoIsNotPublicJpeg(
        string mime,
        string url)
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = new InstagramSocialPublishingBroker(client,
            new() { Enabled = true, AccessToken = "fake_secret", GraphVersion = "v24.0" });

        var result = await broker.PublishAsync(new("123", "Approved", [new([], mime, PublicUrl: url)],
            SaveCheckpointAsync: (_, _) => Task.CompletedTask), Token);

        result.Outcome.Should().Be(BrokerOutcome.Unsupported);
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_ShouldRequirePermissionEvenForTemplate_WhenWhatsAppRecipientConsentIsMissing()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = WhatsApp(client);

        var result = await broker.PublishAsync(
            new("998901234567", "Approved", Template: new("approved_template", "en")),
            Token);

        result.Outcome.Should().Be(BrokerOutcome.Rejected);
        result.Code.Should().Be("permission_required");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_ShouldRequireTemplate_WhenWhatsAppServiceWindowIsUnconfirmed()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = WhatsApp(client);

        var result = await broker.PublishAsync(
            new("998901234567", "Approved", RecipientPermissionConfirmed: true),
            Token);

        result.Outcome.Should().Be(BrokerOutcome.Rejected);
        result.Code.Should().Be("template_required");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_ShouldReturnAcceptanceWithoutClaimingDelivery_WhenWhatsAppTemplateIsAccepted()
    {
        using var handler = new FakeHandler(_ => Json("{\"messages\":[{\"id\":\"wamid.123\"}]}"));
        using var client = new HttpClient(handler);
        var broker = WhatsApp(client);

        var result = await broker.PublishAsync(new("998901234567", "",
            Template: new("approved_template", "en"), RecipientPermissionConfirmed: true), Token);

        result.Outcome.Should().Be(BrokerOutcome.Succeeded);
        (result.Receipt?.Stage).Should().Be("accepted");
        result.Message.Should().Contain("delivery remains unverified");
        using var body = JsonDocument.Parse(handler.Requests.Should().ContainSingle().Which.Body);
        (body.RootElement.GetProperty("type").GetString()).Should().Be("template");
        (body.RootElement.GetProperty("template").GetProperty("name").GetString()).Should().Be("approved_template");
    }

    [Fact]
    public async Task Publish_ShouldAvoidDispatch_WhenCancellationIsAlreadyRequested()
    {
        using var handler = new FakeHandler(_ => Json("{}"));
        using var client = new HttpClient(handler);
        var broker = new TelegramSocialPublishingBroker(
            client,
            new() { Enabled = true, BotToken = "123:fake_secret", BotId = "123", DestinationChatId = "-100123" });
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        var result = await broker.PublishAsync(
            new("-100123", "Approved", SaveCheckpointAsync: (_, _) => Task.CompletedTask, AccountId: "123"),
            cancelled.Token);

        result.Outcome.Should().Be(BrokerOutcome.Rejected);
        handler.Requests.Should().BeEmpty();
    }

    private static WhatsAppSocialPublishingBroker WhatsApp(HttpClient client)
        =>
            new(
                client,
                new() { Enabled = true, AccessToken = "fake_secret", PhoneNumberId = "123", GraphVersion = "v24.0" });

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed record CapturedRequest(
        string Method,
        string Uri,
        string Body,
        string? ContentType,
        string? Authorization,
        string? GoogleKey);

    private sealed class FakeHandler(Func<CapturedRequest, HttpResponseMessage> response) : HttpMessageHandler
    {
        internal List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
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
