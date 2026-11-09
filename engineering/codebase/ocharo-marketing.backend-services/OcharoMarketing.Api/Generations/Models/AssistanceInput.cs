namespace OcharoMarketing.Api.Generations.Models;

/// <summary>Represents a bounded AssistanceInput HTTP command.</summary>
public sealed record AssistanceInput(string Prompt, string Operation = "copy", string Locale = "en");
