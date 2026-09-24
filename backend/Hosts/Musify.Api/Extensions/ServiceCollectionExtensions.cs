using FluentValidation;
using Musify.Api.Authentication;
using Musify.Api.DataTransferObjects.PlayLists;
using Musify.Api.OpenApi;
using Musify.Api.Scalar;
using Musify.Application;
using Musify.Infrastructure.MassTransit;
using Musify.Infrastructure.Persistence;
using Musify.Infrastructure.Services;

namespace Musify.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication(configuration);
        services.AddDatabase(configuration);
        services.AddStorageService(configuration);
        services.AddStreamTicketService(configuration);
        services.AddMassTransitClient(configuration);
        services.AddAuthenticationConfiguration(configuration);
        services.AddOpenApiConfiguration();
        services.AddScalarConfiguration();
        services.AddValidatorsFromAssemblyContaining<CreatePlayListRequest>();

        return services;
    }
}
