using Musify.Application.Configuration;
using Musify.Infrastructure.MassTransit;
using Musify.Infrastructure.Persistence;
using Musify.Infrastructure.Services;
using Musify.Worker;

var builder = Host.CreateApplicationBuilder();

var configuration = builder.Configuration;

var services = builder.Services;

services.AddApplicationStorageConfiguration(configuration);
services.AddPlayListConfiguration(configuration);
services.AddTrackConfiguration(configuration);
services.AddStorageService(configuration);
services.AddAudioTranscoder(configuration);
services.AddPictureService(configuration);
services.AddDatabase(configuration);
services.AddMassTransitConsumers(configuration);
services.AddUploadIntentJobs(configuration);

services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();