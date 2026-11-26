using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace RRMS.Infrastructure.Persistence.Extensions;

public static class DbWarmupExtensions
{
    public static async Task WarmupDatabaseAsync<TContext>(
        this IServiceProvider serviceProvider,
        int connectionCount = 5,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(connectionCount);

        var tasks = Enumerable.Range(0, connectionCount)
            .Select(async _ =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

                await dbContext.Database.OpenConnectionAsync(cancellationToken);

                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
                }
                finally
                {
                    await dbContext.Database.CloseConnectionAsync();
                }
            });

        await Task.WhenAll(tasks);
    }
}