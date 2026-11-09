namespace OcharoMarketing.Application.Posts.Models;

/// <summary>Represents Scan data for the local marketing workflow.</summary>
public sealed record ScanModel(string Summary, string[] Suggestions, string[] Issues);
