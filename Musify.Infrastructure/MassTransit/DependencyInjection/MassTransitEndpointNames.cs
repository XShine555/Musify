namespace Musify.Infrastructure.MassTransit
{
    internal static class MassTransitEndpointNames
    {
        internal static string ExecuteQueue(string endpointName) => $"{endpointName}_execute";

        internal static string CompensateQueue(string endpointName) => $"{endpointName}_compensate";
    }
}
