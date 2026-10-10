namespace Adm.AI.Domain.Entities;

using Adm.AI.Domain.Enums;

public class SdsAnalysis
{
    public Guid Id { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public string AnalysisResultJson { get; set; } = string.Empty;
    public AnalysisStatus Status { get; set; }
    public string PromptVersion { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public long ProcessingTimeMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
