using Hangfire;
using Microsoft.Extensions.Hosting;
using Musify.Application.Configuration;

namespace Musify.Infrastructure.Jobs
{
    /// <summary>Registers every recurring Hangfire job when the host starts.</summary>
    public sealed class RecurringJobsRegistrar(
        IRecurringJobManager recurringJobManager,
        UploadIntentConfiguration uploadIntentConfiguration)
        : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var options = new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc };

            recurringJobManager.AddOrUpdate<DailyMixGenerationJob>(
                "daily-mix-generation", job => job.RunAsync(CancellationToken.None), Cron.Daily(), options);
            recurringJobManager.AddOrUpdate<ListeningHistoryCleanupJob>(
                "listening-history-cleanup", job => job.RunAsync(CancellationToken.None), Cron.Daily(3), options);
            recurringJobManager.AddOrUpdate<UploadIntentExpirationJob>(
                "upload-intent-expiration", job => job.RunAsync(CancellationToken.None),
                EveryCron(uploadIntentConfiguration.ExpirationJobIntervalSeconds), options);
            recurringJobManager.AddOrUpdate<TempUploadsCleanupJob>(
                "temp-uploads-cleanup", job => job.RunAsync(CancellationToken.None),
                EveryCron(uploadIntentConfiguration.TempCleanupJobIntervalSeconds), options);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>Hangfire cron has a one-minute resolution, so the interval is rounded up to whole minutes.</summary>
        internal static string EveryCron(int intervalSeconds)
        {
            var minutes = Math.Max(1, (int)Math.Ceiling(intervalSeconds / 60.0));
            return minutes < 60
                ? $"*/{minutes} * * * *"
                : $"0 */{Math.Max(1, minutes / 60)} * * *";
        }
    }
}
