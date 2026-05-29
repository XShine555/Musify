using FluentValidation;
using Musify.Infrastructure.MassTransit;
using Musify.Infrastructure.Persistence;
using Musify.Application.Configuration;
using Musify.Infrastructure.Services;
using WebApi.Authentication;
using WebApi.DataTransferObjects.PlayLists;
using WebApi.OpenApi;
using WebApi.Scalar;

namespace WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddApplicationStorageConfiguration(configuration);
        services.AddPlayListConfiguration(configuration);
        services.AddTrackConfiguration(configuration);
        services.AddStorageService(configuration);
        services.AddUploadIntentConfiguration(configuration);
        services.AddMassTransitClient(configuration);
        services.AddMediator();
        services.AddAuthenticationConfiguration(configuration);
        services.AddOpenApiConfiguration();
        services.AddScalarConfiguration();
        services.AddValidatorsFromAssemblyContaining<CreatePlayListRequest>();

        return services;
    }
}
