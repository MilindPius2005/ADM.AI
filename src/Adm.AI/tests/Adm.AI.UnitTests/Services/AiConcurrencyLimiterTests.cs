using Adm.AI.Application.Configuration;
using Adm.AI.Application.Services;
using Microsoft.Extensions.Options;

namespace Adm.AI.UnitTests.Services;

public class AiConcurrencyLimiterTests
{
    [Fact]
    public async Task ExecuteAsync_LimitsConcurrency()
    {
        var settings = Options.Create(new AiProcessingSettings { MaxConcurrentRequests = 2 });
        var limiter = new AiConcurrencyLimiter(settings);

        int activeTasks = 0;
        int maxActiveTasks = 0;
        var lockObj = new object();

        var tasks = Enumerable.Range(0, 5).Select(i => limiter.ExecuteAsync(async () =>
        {
            lock (lockObj)
            {
                activeTasks++;
                if (activeTasks > maxActiveTasks) maxActiveTasks = activeTasks;
            }
            await Task.Delay(50);
            lock (lockObj)
            {
                activeTasks--;
            }
            return i;
        })).ToList();

        await Task.WhenAll(tasks);

        Assert.Equal(2, maxActiveTasks);
    }
}
