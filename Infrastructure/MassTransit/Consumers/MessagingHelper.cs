namespace Musify.Infrastructure.MassTransit.Consumers
{
    internal static class MessagingHelper
    {
        internal static Uri BuildExecuteActivityUri(string endpointName) => new($"queue:{endpointName}_execute");

        internal static Uri BuildCompensateActivityUri(string endpointName) => new($"queue:{endpointName}_compensate");

        internal static Uri BuildConsumerUri(string endpointName) => new($"queue:{endpointName}");
    }
}