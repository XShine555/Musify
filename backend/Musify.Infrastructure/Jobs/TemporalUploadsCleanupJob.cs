using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;

namespace Musify.Infrastructure.Jobs
{
    public class TemporalUploadsCleanUpJob(
        IServiceScopeFactory scopeFactory,
        ILogger<TemporalUploadsCleanUpJob> logger,
        ApplicationStorageConfiguration storageConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("TemporalUploadsCleanUpJob started (interval: {Interval}s, prefix: {Prefix} )",
                uploadIntentConfiguration.TemporalCleanUpJobIntervalSeconds,
                uploadIntentConfiguration.TemporalRootPrefix);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(uploadIntentConfiguration.TemporalCleanUpJobIntervalSeconds));

            do
            {
                try
                {
                    await RunAsync(stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Error in TemporalUploadsCleanUpJob");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        async Task RunAsync(CancellationToken cancellationToken)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

            var cutOff = DateTime.UtcNow.AddDays(-uploadIntentConfiguration.TemporalUploadsRetentionDays);
            var deleted = 0;
            var errors = 0;

            await foreach (var (key, lastModified) in storageService.ListObjectsAsync(
                storageConfiguration.Bucket,
                uploadIntentConfiguration.TemporalRootPrefix,
                cancellationToken))
            {
                if (lastModified >= cutOff)
                    continue;

                try
                {
                    await storageService.RemoveFileAsync(storageConfiguration.Bucket, key, cancellationToken);
                    deleted++;
                }
                catch (Exception exception)
                {
                    errors++;
                    logger.LogWarning(exception, "Failed to delete orphaned temp object {Key}", key);
                }
            }

            if (deleted > 0 || errors > 0)
                logger.LogInformation("TemporalUploadsCleanUpJob: deleted {Deleted} orphaned objects, {Errors} errors", deleted, errors);
        }
    }
}
