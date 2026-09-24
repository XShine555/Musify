using Musify.Application.Configuration;
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
services.AddUploadIntentConfiguration(configuration);
services.AddMediator();
services.AddJobs();

var host = builder.Build();

host.Run();
