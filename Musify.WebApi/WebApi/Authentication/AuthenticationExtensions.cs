using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace WebApi.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptionsWithValidateOnStart<AuthenticationConfiguration>()
            .Bind(configuration.GetRequiredSection(AuthenticationConfiguration.SectionName))
            .ValidateDataAnnotations();

        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<AuthenticationConfiguration>>().Value);

        services.AddScoped<JwtBearerEventsHandler>();
        services.ConfigureOptions<JwtBearerOptionsSetup>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddAuthorization();

        return services;
    }
}
