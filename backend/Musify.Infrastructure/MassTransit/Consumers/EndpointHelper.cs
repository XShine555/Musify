namespace Musify.Infrastructure.MassTransit.Consumers
{
    internal static class EndpointHelper
    {
        internal static Uri BuildExecuteActivityUri(string endpointName) => new($"queue:{endpointName}_execute");

        internal static Uri BuildConsumerUri(string endpointName) => new($"queue:{endpointName}");
    }
}
