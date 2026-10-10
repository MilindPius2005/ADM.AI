namespace Adm.AI.Infrastructure.Persistence.Repositories;

using Adm.AI.Domain.Entities;
using Adm.AI.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class AnalysisRepository : IAnalysisRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<AnalysisRepository> _logger;

    public AnalysisRepository(AppDbContext context, ILogger<AnalysisRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SdsAnalysis?> GetByHashAsync(string fileHash, CancellationToken ct = default)
    {
        return await _context.SdsAnalyses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.FileHash == fileHash, ct);
    }

    public async Task<SdsAnalysis?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.SdsAnalyses
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
        try
        {
            _context.SdsAnalyses.Add(analysis);
            await _context.SaveChangesAsync(ct);
            return analysis;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogInformation(
                "Concurrent duplicate resolved: {FileHash} — returning existing analysis",
                analysis.FileHash);

            // Detach the failed entity to avoid tracking conflicts
            _context.Entry(analysis).State = EntityState.Detached;

            // Return the existing record that won the race
            return await _context.SdsAnalyses
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
