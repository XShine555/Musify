using Keycloak.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Identity
{
    public static class KeycloakDependencyInjection
    {
        public static IServiceCollection AddKeycloakService(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddOptionsWithValidateOnStart<KeycloakConfiguration>()
                .Bind(configuration.GetRequiredSection(KeycloakConfiguration.SectionName))
                .ValidateDataAnnotations();

            serviceDescriptors.AddScoped(serviceProvider =>
            {
                var keycloakConfiguration = serviceProvider.GetRequiredService<KeycloakConfiguration>();

                return new KeycloakClient(
                    keycloakConfiguration.BaseUrl,
                    keycloakConfiguration.Username,
                    keycloakConfiguration.Password);
            } );
            serviceDescriptors.AddScoped<IKeycloakUserService, KeycloakUserService>();
            return serviceDescriptors;
        }
    }
}