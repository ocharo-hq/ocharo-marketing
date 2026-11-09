using OcharoMarketing.Application.Deliveries.Models;
using OcharoMarketing.Application.Generations.Models;

namespace OcharoMarketing.Application.Generations.Services;

/// <summary>Defines optional external preparation without coupling product workflows to provider DTOs.</summary>
public interface IExternalMarketingService
{
    IReadOnlyList<ConnectionModel> GetConnections();
    bool CanGenerate(string provider);
    Task<object> AssistAsync(string prompt, string operation, string locale, CancellationToken cancellationToken);
    Task ExecuteGenerationAsync(JobClaim claim, CancellationToken cancellationToken);
}
