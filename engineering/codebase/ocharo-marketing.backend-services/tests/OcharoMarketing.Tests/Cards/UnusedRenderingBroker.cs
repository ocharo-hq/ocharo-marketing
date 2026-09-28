using OcharoMarketing.Application.Cards.Brokers;

namespace OcharoMarketing.Tests.Cards;

/// <summary>Rejects unexpected browser work in pure composition tests.</summary>
internal sealed class UnusedRenderingBroker : IDocumentRenderingBroker
{
    public Task<byte[]> RenderPngAsync(string html, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("This composition test must not launch a browser.");
    }

    public Task<byte[]> RenderPdfAsync(string html, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("This composition test must not launch a browser.");
    }
}
