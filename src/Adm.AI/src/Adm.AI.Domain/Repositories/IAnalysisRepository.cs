namespace Adm.AI.Domain.Repositories;

using Adm.AI.Domain.Entities;

public interface IAnalysisRepository
{
    /// <summary>
    /// Returns existing analysis for the given hash, or null if not found.
    /// </summary>
    Task<SdsAnalysis?> GetByHashAsync(string fileHash, CancellationToken ct = default);

    /// <summary>
    /// Returns existing analysis by ID, or null if not found.
    /// </summary>
    Task<SdsAnalysis?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Inserts a new analysis. If a unique constraint violation occurs on file_hash
    /// (concurrent duplicate), returns the existing record instead.
    /// </summary>
    Task<SdsAnalysis> CreateAsync(SdsAnalysis analysis, CancellationToken ct = default);
}
