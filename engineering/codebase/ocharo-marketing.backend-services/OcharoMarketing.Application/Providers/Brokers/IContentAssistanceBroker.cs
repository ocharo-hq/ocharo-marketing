namespace OcharoMarketing.Application.Providers;

/// <summary>Integrates an external text and vision assistance capability.</summary>
public interface IContentAssistanceBroker
{
    string Provider { get; }
    Task<ContentAssistanceResult> AssistAsync(ContentAssistanceRequest request, CancellationToken cancellationToken);
}
