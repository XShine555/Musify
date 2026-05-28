using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Domain.ValueObjects;
using Musify.Application.Contracts;

namespace Musify.Infrastructure.Jobs
{
    public class UploadIntentExpirationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<UploadIntentExpirationJob> logger,
        UploadIntentConfiguration uploadIntentConfiguration)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("UploadIntentExpirationJob started (interval: {Interval}s)", uploadIntentConfiguration.ExpirationJobIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunAsync(stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Error in UploadIntentExpirationJob");
                }

                await Task.Delay(TimeSpan.FromSeconds(uploadIntentConfiguration.ExpirationJobIntervalSeconds), stoppingToken);
            }
        }

        async Task RunAsync(CancellationToken cancellationToken)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<IDatabase>();

            var now = DateTime.UtcNow;

            // Mark expired intents
            var expiredIntents = await database.UploadIntents
                .Where(i => i.Status == UploadIntentStatus.Issued && i.ExpiresAt < now)
                .ToListAsync(cancellationToken);

            if (expiredIntents.Count > 0)
            {
                foreach (var intent in expiredIntents)
                    intent.Status = UploadIntentStatus.Expired;

                await database.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Marked {Count} upload intents as Expired", expiredIntents.Count);
            }

            // Delete old Expired intents past retention window
            if (uploadIntentConfiguration.ExpiredIntentsRetentionDays >= 0)
            {
                var retentionCutoff = now.AddDays(-uploadIntentConfiguration.ExpiredIntentsRetentionDays);

                var oldExpiredIntents = await database.UploadIntents
                    .Where(i => i.Status == UploadIntentStatus.Expired && i.ExpiresAt < retentionCutoff)
                    .ToListAsync(cancellationToken);

                if (oldExpiredIntents.Count > 0)
                {
                    database.UploadIntents.RemoveRange(oldExpiredIntents);
                    await database.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Deleted {Count} old Expired upload intents past retention window", oldExpiredIntents.Count);
                }
            }
        }
    }
}
