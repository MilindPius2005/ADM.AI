namespace Adm.AI.Application.Interfaces;

public interface IAiConcurrencyLimiter
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
}
