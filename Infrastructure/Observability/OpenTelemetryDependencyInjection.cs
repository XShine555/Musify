using Microsoft.Extensions.DependencyInjection;

namespace Musify.Infrastructure.Observability
{
    public static class OpenTelemetryDependencyInjection
    {
        public static IServiceCollection AddInfrastructureOpenTelemetry(this IServiceCollection serviceDescriptors)
        {
            serviceDescriptors
                .AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing.AddSource("MassTransit");
                } )
                .WithMetrics(metrics =>
                {
                    metrics.AddMeter("MassTransit");
                } );

            return serviceDescriptors;
        }
    }
}
