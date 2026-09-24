using Hangfire;
using Musify.Application.Configuration;
using Musify.Infrastructure.Jobs;
using Musify.Infrastructure.MassTransit;
using Musify.Infrastructure.Persistence;
using Musify.Infrastructure.Services;

var builder = Host.CreateApplicationBuilder();

builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
{
    ValidateOnBuild = false,
    ValidateScopes = true
}));

var configuration = builder.Configuration;

var services = builder.Services;

services.AddValidatedOptions<ApplicationStorageConfiguration>(configuration);
services.AddValidatedOptions<PlayListConfiguration>(configuration);
services.AddValidatedOptions<TrackConfiguration>(configuration);
services.AddValidatedOptions<MixConfiguration>(configuration);
services.AddStorageService(configuration);
services.AddAudioTranscoder(configuration);
services.AddPictureService();
services.AddDatabase(configuration);
services.AddMassTransitConsumers(configuration);
services.AddUploadIntentJobs(configuration);
services.AddMediator();
services.AddDailyMixGenerationJob(configuration);

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<DailyMixGenerationJob>(
        "daily-mix-generation",
        job => job.RunAsync(CancellationToken.None),
        Cron.Daily(),
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    recurringJobManager.AddOrUpdate<ListeningHistoryCleanupJob>(
        "listening-history-cleanup",
        job => job.RunAsync(CancellationToken.None),
        Cron.Daily(3),
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}

host.Run();
