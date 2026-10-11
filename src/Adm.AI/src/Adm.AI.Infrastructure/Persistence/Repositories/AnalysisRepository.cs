namespace Adm.AI.Infrastructure.Persistence.Repositories;

using Adm.AI.Domain.Entities;
using Adm.AI.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class AnalysisRepository : IAnalysisRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<AnalysisRepository> _logger;

    public AnalysisRepository(IDbContextFactory<AppDbContext> contextFactory, ILogger<AnalysisRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<SdsAnalysis?> GetByHashAsync(string fileHash, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SdsAnalyses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.FileHash == fileHash, ct);
    }

    public async Task<SdsAnalysis?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SdsAnalyses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    /// <summary>
    /// Inserts a new analysis. If a unique constraint violation occurs on file_hash
    /// (concurrent duplicate), returns the existing record instead.
    /// This pattern is lock-free, race-condition safe, and idempotent.
    /// </summary>
    public async Task<SdsAnalysis> CreateAsync(SdsAnalysis analysis, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        try
        {
            context.SdsAnalyses.Add(analysis);
            await context.SaveChangesAsync(ct);
            return analysis;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogInformation(
                "Concurrent duplicate resolved: {FileHash} — returning existing analysis",
                analysis.FileHash);

            // Detach the failed entity to avoid tracking conflicts
            context.Entry(analysis).State = EntityState.Detached;

            // Return the existing record that won the race
            return await context.SdsAnalyses
                .AsNoTracking()
                .FirstAsync(a => a.FileHash == analysis.FileHash, ct);
        }
    }

    /// <summary>
    /// Detects PostgreSQL unique constraint violation (error code 23505).
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        // Npgsql wraps PostgreSQL errors; the SqlState for unique violation is "23505"
        return ex.InnerException is Npgsql.PostgresException pgEx
            && pgEx.SqlState == "23505";
    }
}
