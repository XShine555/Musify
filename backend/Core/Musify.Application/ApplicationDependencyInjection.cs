using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Configuration;
using Musify.Application.Services;

namespace Musify.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediator();
        services.AddScoped<UploadIntentValidator>();
        services.AddScoped<TrackStreamIssuer>();

        services.AddValidatedOptions<ApplicationStorageConfiguration>(configuration, ApplicationStorageConfiguration.SectionName);
        services.AddValidatedOptions<PlayListConfiguration>(configuration, PlayListConfiguration.SectionName);
        services.AddValidatedOptions<AlbumConfiguration>(configuration, AlbumConfiguration.SectionName);
        services.AddValidatedOptions<TrackConfiguration>(configuration, TrackConfiguration.SectionName);
        services.AddValidatedOptions<MixConfiguration>(configuration, MixConfiguration.SectionName);
        services.AddValidatedOptions<StreamGatewayConfiguration>(configuration, StreamGatewayConfiguration.SectionName);
        services.AddValidatedOptions<UploadIntentConfiguration>(configuration, UploadIntentConfiguration.SectionName);
        services.AddValidatedOptions<PlaybackConfiguration>(configuration, PlaybackConfiguration.SectionName);

        return services;
    }
}
