using Microsoft.AspNetCore.OpenApi;

namespace Musify.Api.OpenApi;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiConfiguration(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.ConfigureOptions<OpenApiOptionsSetup>();

        return services;
    }
}
