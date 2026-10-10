namespace Adm.AI.Application.DTOs;

/// <summary>
/// Generic API response wrapper — used for ALL endpoints.
/// </summary>
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public bool FromCache { get; init; }
    public string? AnalysisId { get; init; }
    public long? ProcessingTimeMs { get; init; }
    public T? Data { get; init; }
    public ErrorDetail? Error { get; init; }

    public static ApiResponse<T> Ok(T data, string analysisId, long processingTimeMs, bool fromCache) => new()
    {
        Success = true,
        FromCache = fromCache,
        AnalysisId = analysisId,
        ProcessingTimeMs = processingTimeMs,
        Data = data
    };

    public static ApiResponse<T> Fail(string code, string message) => new()
    {
        Success = false,
        Error = new ErrorDetail(code, message)
    };
}
