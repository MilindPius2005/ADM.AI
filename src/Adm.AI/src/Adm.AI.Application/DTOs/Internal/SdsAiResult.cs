namespace Adm.AI.Application.DTOs.Internal;

using Adm.AI.Domain.Models;

/// <summary>
/// Returned by ISdsAiProcessingService. Contains deserialized extraction result and usage metrics.
/// </summary>
public record SdsAiResult(
    SdsExtractionResult ExtractionResult,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal EstimatedCostUsd,
    long AiProcessingTimeMs
);
