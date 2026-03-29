using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Musify.Infrastructure.Configuration;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SixLabors.ImageSharp;

namespace Musify.Infrastructure.Observability
{
    public static class OpenTelemetryDependencyInjection
    {
        public static IServiceCollection AddInfrastructureOpenTelemetry(this IServiceCollection serviceDescriptors, IConfiguration configuration)
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
                .WithTracing(tracing =>
                {
                    tracing.AddSource("MassTransit");
                    tracing.AddOtlpExporter();
                } )
                .WithMetrics(metrics =>
                {
                    metrics.AddMeter("MassTransit");
                    metrics.AddOtlpExporter();
                } );

            return serviceDescriptors;
        }
    }
}