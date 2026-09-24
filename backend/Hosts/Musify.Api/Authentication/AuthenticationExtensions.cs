using Microsoft.AspNetCore.Authentication.JwtBearer;
using Musify.Application.Configuration;

namespace Musify.Api.Authentication
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddAuthenticationConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddValidatedOptions<AuthenticationConfiguration>(configuration);

            services.AddMemoryCache();
            services.AddHttpClient();
            services.AddScoped<JwtBearerEventsHandler>();
            services.ConfigureOptions<JwtBearerOptionsSetup>();

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services.AddAuthorization();

            return services;
        }
    }
}
