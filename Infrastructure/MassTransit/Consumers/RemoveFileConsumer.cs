using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Events;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class RemoveFileConsumer(
        IStorageService storageService,
        ILogger<RemoveFileConsumer> logger)
        : IConsumer<RemoveFileEvent>
    {
        public const string QueueName = "remove-file";

        public async Task Consume(ConsumeContext<RemoveFileEvent> consumeContext)
        {
            try
            {
                await storageService.RemoveFileAsync(
                    consumeContext.Message.Bucket,
                    consumeContext.Message.Key,
                    consumeContext.CancellationToken);

                logger.LogInformation(
                    "Removed file {Key} from bucket {Bucket}",
                    consumeContext.Message.Key,
                    consumeContext.Message.Bucket);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to remove file {Key} from bucket {Bucket}",
                    consumeContext.Message.Key,
                    consumeContext.Message.Bucket);
                throw;
            }
        }
    }
}
