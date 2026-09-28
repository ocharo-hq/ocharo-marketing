namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Represents Scan data for the local marketing workflow.</summary>
public sealed record ScanModel(string Summary, string[] Suggestions, string[] Issues);
