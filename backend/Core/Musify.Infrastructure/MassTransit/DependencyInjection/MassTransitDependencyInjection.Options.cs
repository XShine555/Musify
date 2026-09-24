using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Musify.Infrastructure.MassTransit;

public static partial class MassTransitDependencyInjection
{
    private static void RegisterValidatedOptions<TOptions>(
        IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        services
            .AddOptionsWithValidateOnStart<TOptions>()
            .Bind(configuration.GetRequiredSection(sectionName))
            .ValidateDataAnnotations();

        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<TOptions>>().Value);
    }
}
