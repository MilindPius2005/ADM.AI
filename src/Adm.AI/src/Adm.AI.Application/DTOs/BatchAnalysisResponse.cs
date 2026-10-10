namespace Adm.AI.Application.DTOs;

public record BatchAnalysisResponse
{
    public int Total { get; init; }
    public int Successful { get; init; }
    public int Failed { get; init; }
    public long TotalProcessingTimeMs { get; init; }
    public List<BatchItemResult> Results { get; init; } = [];
}

public record BatchItemResult
{
    public string FileName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public bool FromCache { get; init; }
    public string? AnalysisId { get; init; }
    public long? ProcessingTimeMs { get; init; }
    public SdsAnalysisResponse? Data { get; init; }
    public ErrorDetail? Error { get; init; }
}
