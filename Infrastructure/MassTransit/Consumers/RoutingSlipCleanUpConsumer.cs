using MassTransit;
using MassTransit.Courier.Contracts;
using Microsoft.Extensions.Logging;
using Musify.Infrastructure.Messaging;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class RoutingSlipCleanUpConsumer(
        ILogger<RoutingSlipCleanUpConsumer> logger) :
        IConsumer<RoutingSlipCompleted>,
        IConsumer<RoutingSlipFaulted>
    {
        public const string QueueName = "routing-slip-events";

        public Task Consume(ConsumeContext<RoutingSlipCompleted> consumeContext)
            => DeleteFolder(consumeContext.Message.Variables);

        public Task Consume(ConsumeContext<RoutingSlipFaulted> consumeContext)
            => DeleteFolder(consumeContext.Message.Variables);

        Task DeleteFolder(IDictionary<string, object> variables) 
        {
            if (!variables.TryGetValue(RoutingSlipVariableNames.Workflow.TemporalDirectory, out var pathObject))
                return Task.CompletedTask;

            var pathString = pathObject.ToString();

            if (string.IsNullOrEmpty(pathString))
                return Task.CompletedTask;

            try
            {
                Directory.Delete(pathString, recursive: true);
                logger.LogInformation("Deleted temporal directory at path {Path}", pathString);
            }
            catch (DirectoryNotFoundException directoryNotFoundException)
            {
                logger.LogWarning(directoryNotFoundException, "Temporal directory at path {Path} was not found for deletion", pathString);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to delete temporal directory at path {Path}", pathString);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}