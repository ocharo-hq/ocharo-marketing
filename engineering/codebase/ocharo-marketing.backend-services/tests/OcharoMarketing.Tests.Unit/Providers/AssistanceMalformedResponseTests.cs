using System.Net;
using System.Text;
using AwesomeAssertions;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Infrastructure.Providers.Brokers;
using Xunit;

namespace OcharoMarketing.Tests.Unit.Providers;

public sealed class AssistanceMalformedResponseTests
{
    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("\"fake_secret\"")]
    [InlineData("42")]
    [InlineData("{}")]
    [InlineData("{\"output\":null}")]
    [InlineData("{\"output\":{}}")]
    public async Task Assist_ShouldReturnUnknownWithoutText_WhenAcceptedBodyHasNoUsableOutput(string response)
    {
        using var handler = new FakeHandler(response);
        using var client = new HttpClient(handler);
        var broker = new OpenAiContentAssistanceBroker(client, new() { Enabled = true, ApiKey = "fake_secret" });

        var result = await broker.AssistAsync(new("Analyze visible garment"), CancellationToken.None);

        result.Outcome.Should().Be(BrokerOutcome.Ambiguous);
        result.Code.Should().Be("missing_text");
        result.Text.Should().BeNull();
        result.Receipt.Should().BeNull();
        result.Message.Should().NotContain("fake_secret");
        handler.Calls.Should().Be(1);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("\"fake_secret\"")]
    [InlineData("42")]
    public async Task Generate_ShouldReturnUnknownWithoutArtifacts_WhenPaidResponseRootIsMalformed(string response)
    {
        using var handler = new FakeHandler(response);
        using var client = new HttpClient(handler);
        IImageGenerationBroker[] brokers =
        [
            new OpenAiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" }),
            new GeminiImageGenerationBroker(client, new() { Enabled = true, ApiKey = "fake_secret" })
        ];

        foreach (var broker in brokers)
        {
            var result = await broker.GenerateAsync(new("Garment scene"), CancellationToken.None);
            result.Outcome.Should().Be(BrokerOutcome.Ambiguous);
            result.Images.Should().BeEmpty();
            result.Message.Should().NotContain("fake_secret");
        }
        handler.Calls.Should().Be(2);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("\"fake_secret\"")]
    [InlineData("42")]
    public async Task Publish_ShouldReturnUnknownWithoutReceipt_WhenWhatsAppSuccessRootIsMalformed(string response)
    {
        using var handler = new FakeHandler(response);
        using var client = new HttpClient(handler);
        var broker = new WhatsAppSocialPublishingBroker(client,
            new() { Enabled = true, AccessToken = "fake_secret", PhoneNumberId = "123", GraphVersion = "v24.0" });
        var result = await broker.PublishAsync(new("998901234567", "Approved",
            CustomerServiceWindowConfirmed: true, RecipientPermissionConfirmed: true), CancellationToken.None);
        result.Outcome.Should().Be(BrokerOutcome.Ambiguous);
        result.Code.Should().Be("missing_receipt");
        result.Receipt.Should().BeNull();
        result.Message.Should().NotContain("fake_secret");
        handler.Calls.Should().Be(1);
    }

    private sealed class FakeHandler(string body) : HttpMessageHandler
    {
        internal int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
