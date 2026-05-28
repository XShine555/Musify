using FluentValidation;
using Mediator;
using Musify.Application;
using Musify.Infrastructure.MassTransit;
using Musify.Infrastructure.Persistence;
using Musify.Application.Configuration;
using Musify.Infrastructure.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using WebApi.DataTransferObjects.PlayLists;
using WebApi.Options;

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
        services.AddOpenApi();
        services.AddValidatorsFromAssemblyContaining<CreatePlayListRequest>();

        return services;
    }

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var otlpEndpoint = new Uri(configuration
            .GetRequiredSection(OpenTelemetryOptions.SectionName)
            .GetRequiredSection(nameof(OpenTelemetryOptions.OtlpEndpoint))
            .Value!);

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = otlpEndpoint))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = otlpEndpoint));

        services.Configure<OpenTelemetryLoggerOptions>(logging =>
            logging.AddOtlpExporter(o => o.Endpoint = otlpEndpoint));

        return services;
    }
}
