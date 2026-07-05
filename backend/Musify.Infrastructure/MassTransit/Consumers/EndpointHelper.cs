namespace Musify.Infrastructure.MassTransit.Consumers
{
    internal static class EndpointHelper
    {
        internal static string ExecuteQueueName(string endpointName) => $"{endpointName}_execute";

        internal static string CompensateQueueName(string endpointName) => $"{endpointName}_compensate";

        internal static Uri BuildExecuteActivityUri(string endpointName) => new($"queue:{ExecuteQueueName(endpointName)}");

        internal static Uri BuildCompensateActivityUri(string endpointName) => new($"queue:{CompensateQueueName(endpointName)}");

        internal static Uri BuildConsumerUri(string endpointName) => new($"queue:{endpointName}");
    }
}
