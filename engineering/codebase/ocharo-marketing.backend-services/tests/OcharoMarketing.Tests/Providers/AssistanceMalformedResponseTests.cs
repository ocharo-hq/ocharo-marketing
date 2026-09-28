using System.Net;
using System.Text;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Infrastructure.Providers.Brokers;
using Xunit;

namespace OcharoMarketing.Tests.Providers;

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

        Assert.Equal(BrokerOutcome.Ambiguous, result.Outcome);
        Assert.Equal("missing_text", result.Code);
        Assert.Null(result.Text);
        Assert.Null(result.Receipt);
        Assert.DoesNotContain("fake_secret", result.Message);
        Assert.Equal(1, handler.Calls);
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
            Assert.Equal(BrokerOutcome.Ambiguous, result.Outcome);
            Assert.Empty(result.Images);
            Assert.DoesNotContain("fake_secret", result.Message);
        }
        Assert.Equal(2, handler.Calls);
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
        Assert.Equal(BrokerOutcome.Ambiguous, result.Outcome);
        Assert.Equal("missing_receipt", result.Code);
        Assert.Null(result.Receipt);
        Assert.DoesNotContain("fake_secret", result.Message);
        Assert.Equal(1, handler.Calls);
    }

    private sealed class FakeHandler(string body) : HttpMessageHandler
    {
        internal int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
