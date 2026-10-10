namespace Adm.AI.Application.Configuration;

using System.ComponentModel.DataAnnotations;

public class OpenAiSettings
{
    public const string SectionName = "OpenAi";

    [Required] public string ApiKey { get; set; } = string.Empty;
    [Required] public string Model { get; set; } = "gpt-4o";
    public string? EndpointUrl { get; set; }
    public int MaxTokens { get; set; } = 16384;
    public float Temperature { get; set; } = 0f;
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxRetries { get; set; } = 2;
    public decimal InputTokenPricePerMillion { get; set; } = 2.50m;
    public decimal OutputTokenPricePerMillion { get; set; } = 10.00m;
}
