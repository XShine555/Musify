using Musify.Application.Configuration;
using Musify.Infrastructure.MassTransit;
using Musify.Infrastructure.Persistence;
using Musify.Infrastructure.Services;

var builder = Host.CreateApplicationBuilder();

var configuration = builder.Configuration;

var services = builder.Services;

services.AddValidatedOptions<ApplicationStorageConfiguration>(configuration, ApplicationStorageConfiguration.SectionName);
services.AddValidatedOptions<PlayListConfiguration>(configuration, PlayListConfiguration.SectionName);
services.AddValidatedOptions<TrackConfiguration>(configuration, TrackConfiguration.SectionName);
services.AddStorageService(configuration);
services.AddAudioTranscoder(configuration);
services.AddPictureService(configuration);
services.AddDatabase(configuration);
services.AddMassTransitConsumers(configuration);
services.AddUploadIntentJobs(configuration);

var host = builder.Build();

host.Run();