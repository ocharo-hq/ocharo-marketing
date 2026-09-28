namespace OcharoMarketing.Application.Cards.Brokers;

/// <summary>Defines offline browser rendering of an application-owned document.</summary>
public interface IDocumentRenderingBroker
{
    Task<byte[]> RenderPngAsync(string html, CancellationToken cancellationToken);
    Task<byte[]> RenderPdfAsync(string html, CancellationToken cancellationToken);
}
