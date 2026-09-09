using Hangfire;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Configuration;
using Musify.Application.Services;
using Musify.Infrastructure.Jobs;
using Musify.Infrastructure.MassTransit;
using Musify.Infrastructure.Persistence;
using Musify.Infrastructure.Services;

var builder = Host.CreateApplicationBuilder();

// Registering Mediator pulls in every handler in Musify.Application, including ones
// (e.g. CreatePlayList, CreateTrack) that depend on IEventBus, which the Worker never
// registers because it only consumes events, it never publishes them. Only the handlers
// this process actually dispatches (GenerateMixesForUserCommand) are ever constructed,
// so the default eager validate-on-build check is too strict for this host.
builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
{
    ValidateOnBuild = false,
    ValidateScopes = true
}));

var configuration = builder.Configuration;

var services = builder.Services;

services.AddValidatedOptions<ApplicationStorageConfiguration>(configuration, ApplicationStorageConfiguration.SectionName);
services.AddValidatedOptions<PlayListConfiguration>(configuration, PlayListConfiguration.SectionName);
services.AddValidatedOptions<TrackConfiguration>(configuration, TrackConfiguration.SectionName);
services.AddValidatedOptions<MixConfiguration>(configuration, MixConfiguration.SectionName);
services.AddStorageService(configuration);
services.AddAudioTranscoder(configuration);
services.AddYouTubeDownloader(configuration);
services.AddHttpClient();
services.AddPictureService(configuration);
services.AddPlayListPresetSeeder(configuration);
services.AddDatabase(configuration);
services.AddMassTransitConsumers(configuration);
services.AddUploadIntentJobs(configuration);
services.AddMediator();
services.AddScoped<YouTubeTrackProvisioner>();
services.AddYouTubeMusicService(configuration);
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
}

host.Run();
