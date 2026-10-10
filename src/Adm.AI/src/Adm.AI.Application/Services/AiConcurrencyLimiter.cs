using Adm.AI.Application.Configuration;
using Adm.AI.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Adm.AI.Application.Services;

public class AiConcurrencyLimiter : IAiConcurrencyLimiter
{
    private readonly SemaphoreSlim _semaphore;

    public AiConcurrencyLimiter(IOptions<AiProcessingSettings> settings)
    {
        _semaphore = new SemaphoreSlim(settings.Value.MaxConcurrentRequests, settings.Value.MaxConcurrentRequests);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            return await action();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
