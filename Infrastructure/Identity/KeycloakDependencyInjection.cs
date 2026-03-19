using Keycloak.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Identity
{
    public static class KeycloakDependencyInjection
    {
        public static void AddKeycloakAuthentication(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            var keycloakConfiguration = serviceDescriptors.AddOptionsWithValidateOnStart<KeycloakConfiguration>()
                .Bind(configuration)
                .ValidateDataAnnotations();

            serviceDescriptors.AddScoped(serviceProvider =>
            {
                var keycloakConfiguration = serviceProvider.GetRequiredService<KeycloakConfiguration>();

                return new KeycloakClient(
                    keycloakConfiguration.BaseUrl,
                    keycloakConfiguration.Username,
                    keycloakConfiguration.Password);
            } );
            serviceDescriptors.AddScoped<IKeycloakUserClient, KeycloakIdentityService>();
        }
    }
}