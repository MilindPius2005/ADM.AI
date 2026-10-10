namespace Adm.AI.Application.DTOs;

using Adm.AI.Domain.Models;

/// <summary>
/// Analysis response — the T in ApiResponse&lt;T&gt;.
/// Entities never exposed; this DTO is the only shape that leaves the Application layer.
/// </summary>
public record SdsAnalysisResponse
{
    public string AnalysisId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string FileHash { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string PromptVersion { get; init; } = string.Empty;
    public string OpenAiModel { get; init; } = string.Empty;
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens { get; init; }
    public decimal EstimatedCostUsd { get; init; }
    public long ProcessingTimeMs { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public SdsExtractionResult ExtractionResult { get; init; } = null!;
}
