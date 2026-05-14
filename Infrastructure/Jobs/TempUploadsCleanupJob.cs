using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Configuration;

namespace Musify.Infrastructure.Jobs
{
    public class TempUploadsCleanupJob(
        IServiceScopeFactory scopeFactory,
        ILogger<TempUploadsCleanupJob> logger,
        ApplicationStorageConfiguration storageConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("TempUploadsCleanupJob started (interval: {Interval}s, prefix: {Prefix})",
                uploadIntentConfiguration.TempCleanupJobIntervalSeconds,
                uploadIntentConfiguration.TempRootPrefix);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunAsync(stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Error in TempUploadsCleanupJob");
                }

                await Task.Delay(TimeSpan.FromSeconds(uploadIntentConfiguration.TempCleanupJobIntervalSeconds), stoppingToken);
            }
        }

        async Task RunAsync(CancellationToken cancellationToken)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

            var cutoff = DateTime.UtcNow.AddDays(-uploadIntentConfiguration.TempUploadsRetentionDays);
            var deleted = 0;
            var errors = 0;

            await foreach (var (key, lastModified) in storageService.ListObjectsAsync(
                storageConfiguration.Bucket,
                uploadIntentConfiguration.TempRootPrefix,
                cancellationToken))
            {
                if (lastModified >= cutoff)
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
                logger.LogInformation("TempUploadsCleanupJob: deleted {Deleted} orphaned objects, {Errors} errors", deleted, errors);
        }
    }
}
