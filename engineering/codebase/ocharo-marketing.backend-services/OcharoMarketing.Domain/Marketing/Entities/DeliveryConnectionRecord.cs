namespace OcharoMarketing.Domain.Marketing.Entities;

/// <summary>Persists delivery evidence independently of provider secrets.</summary>
public sealed class DeliveryConnectionRecord
{
    public string Id { get; set; } = "";
    public int Revision { get; set; }
    public string VerificationStatus { get; set; } = "not-verified";
    public string SafeMessage { get; set; } = "";
    public string ConfigurationStamp { get; set; } = "";
    public string Json { get; set; } = "";
}
