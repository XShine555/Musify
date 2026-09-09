using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Musify.Infrastructure.Configuration;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Musify.Infrastructure.Observability
{
    public static class OpenTelemetryDependencyInjection
    {
        public static IServiceCollection AddObservability(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddOptionsWithValidateOnStart<OpenTelemetryConfiguration>()
                .Bind(configuration.GetRequiredSection(OpenTelemetryConfiguration.SectionName))
                .ValidateDataAnnotations();

            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<OpenTelemetryConfiguration>>().Value);

            serviceDescriptors
                .AddOptions<OtlpExporterOptions>()
                .Configure<OpenTelemetryConfiguration>((exporterOptions, openTelemetryConfiguration) =>
                {
                    exporterOptions.Endpoint = new Uri(openTelemetryConfiguration.OtlpEndpoint);
                } );

            serviceDescriptors
                .AddOpenTelemetry()
                .ConfigureResource(resource =>
                {
                    resource.AddService("Musify");
                } )
                .WithTracing(tracing =>
                    tracing.AddSource("MassTransit")
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddEntityFrameworkCoreInstrumentation()
                        .AddOtlpExporter()
                )
                .WithMetrics(metrics =>
                    metrics.AddMeter("MassTransit")
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        .AddOtlpExporter()
                )
                .WithLogging(logging =>
                    logging.AddOtlpExporter()
                );

            return serviceDescriptors;
        }
    }
}