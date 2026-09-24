using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Musify.Application.Configuration;

public static class ConfigurationDependencyInjection
{
    public static IServiceCollection AddValidatedOptions<T>(this IServiceCollection services, IConfiguration configuration)
        where T : class, IConfigurationOptions =>
        services.AddValidatedOptions<T>(configuration, T.SectionName);

    public static IServiceCollection AddValidatedOptions<T>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where T : class
    {
        services
            .AddOptionsWithValidateOnStart<T>()
            .Bind(configuration.GetRequiredSection(sectionName))
            .ValidateDataAnnotations();

        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<T>>().Value);

        return services;
    }
}
