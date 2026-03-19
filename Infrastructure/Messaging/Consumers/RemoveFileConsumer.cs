using MassTransit;
using Musify.Application.Events;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class RemoveFileConsumer
        : IConsumer<RemoveFileEvent>
    {
        public Task Consume(ConsumeContext<RemoveFileEvent> context)
        {
            throw new NotImplementedException();
        }
    }
}