namespace Adm.AI.Application.Interfaces;

using Adm.AI.Application.DTOs;

public interface ISdsAnalysisService
{
    /// <summary>
    /// Full pipeline: validate → hash → cache-check → extract → clean → AI → store → return DTO.
    /// Controller passes raw file data. No IFormFile dependency in Application layer.
    /// </summary>
    Task<ApiResponse<SdsAnalysisResponse>> AnalyzeSdsAsync(
        string fileName,
        byte[] fileBytes,
        string? contentType,
        CancellationToken ct = default);

    /// <summary>
    /// Analyze a batch of files with global concurrency limiting.
    /// </summary>
    Task<BatchAnalysisResponse> AnalyzeBatchAsync(
        List<(string FileName, byte[] FileBytes, string? ContentType)> files,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieve a previously stored analysis by ID.
    /// </summary>
    Task<ApiResponse<SdsAnalysisResponse>> GetAnalysisAsync(
        Guid id,
        CancellationToken ct = default);
}
