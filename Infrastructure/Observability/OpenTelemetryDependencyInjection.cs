using Microsoft.Extensions.DependencyInjection;
using Musify.Infrastructure.Configuration;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Musify.Infrastructure.Observability
{
    public static class OpenTelemetryDependencyInjection
    {
        public static IServiceCollection AddInfrastructureOpenTelemetry(this IServiceCollection serviceDescriptors)
        {
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