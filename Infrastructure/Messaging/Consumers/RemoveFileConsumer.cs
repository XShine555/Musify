using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class RemoveFileConsumer(IBus bus)
        : IConsumer<RemoveFileEvent>
    {
        public const string QueueName = "Remove-File";

        public async Task Consume(ConsumeContext<RemoveFileEvent> consumeContext)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());

            routingSlipBuilder.AddActivity(
                "RemoveFile",
                BuildExecuteUri(RemoveFilesActivity.ExecuteEndpointName),
                new RemoveFileArguments(
                    consumeContext.Message.BucketName,
                    consumeContext.Message.KeyName));

            var routingSlip = routingSlipBuilder.Build();
            await bus.Execute(routingSlip);
        }

        static Uri BuildExecuteUri(string endpointName) => new($"queue:{endpointName}_execute");
    }
}
