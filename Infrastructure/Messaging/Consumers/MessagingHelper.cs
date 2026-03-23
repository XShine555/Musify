namespace Musify.Infrastructure.Messaging.Consumers
{
    internal static class MessagingHelper
    {
        internal static Uri BuildExecuteActivityUri(string endpointName) => new($"queue:{endpointName}_execute");
    }
}