using Musify.Application;
using Musify.Infrastructure.MassTransit;
using Musify.Infrastructure.Observability;
using Musify.Infrastructure.Persistence;
using Musify.Infrastructure.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
{
    ValidateOnBuild = true,
    ValidateScopes = true
}));

builder.Services
    .AddApplication(builder.Configuration)
    .AddObservability(builder.Configuration)
    .AddDatabase(builder.Configuration)
    .AddStorageService(builder.Configuration)
    .AddAudioTranscoder(builder.Configuration)
    .AddStreamTicketService(builder.Configuration)
    .AddPictureService()
    .AddMassTransitConsumers(builder.Configuration)
    .AddJobs();

var host = builder.Build();

host.Run();
