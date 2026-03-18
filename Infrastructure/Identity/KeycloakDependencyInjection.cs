using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts.Infrastructure;

namespace Musify.Infrastructure.Identity
{
    public static class KeycloakDependencyInjection
    {
        public static void AddKeycloakAuthentication(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            var keycloakConfiguration = KeycloakConfiguration.Load(configuration);

            serviceDescriptors.AddSingleton(keycloakConfiguration);

            serviceDescriptors.AddScoped<IKeycloakUserClient, KeycloakIdentityService>();
        }
    }
}