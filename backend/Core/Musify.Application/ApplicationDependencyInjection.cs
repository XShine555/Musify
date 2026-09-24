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

        services.AddValidatedOptions<ApplicationStorageConfiguration>(configuration);
        services.AddValidatedOptions<PlayListConfiguration>(configuration);
        services.AddValidatedOptions<AlbumConfiguration>(configuration);
        services.AddValidatedOptions<TrackConfiguration>(configuration);
        services.AddValidatedOptions<MixConfiguration>(configuration);
        services.AddValidatedOptions<StreamGatewayConfiguration>(configuration);
        services.AddValidatedOptions<UploadIntentConfiguration>(configuration);
        services.AddValidatedOptions<PlaybackConfiguration>(configuration);

        return services;
    }
}
