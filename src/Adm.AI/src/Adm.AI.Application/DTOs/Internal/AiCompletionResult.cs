namespace Adm.AI.Application.DTOs.Internal;

/// <summary>
/// Returned by IOpenAiProvider. Contains raw JSON content and usage metrics.
/// </summary>
public record AiCompletionResult(
    string JsonContent,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal EstimatedCostUsd,
    long DurationMs
);
