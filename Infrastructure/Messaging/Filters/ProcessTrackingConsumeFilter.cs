using MassTransit;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;

namespace Musify.Infrastructure.Messaging.Filters
{
    public class ProcessTrackingConsumeFilter<TMessage>(IProcessTrackingStore processTrackingStore)
        : IFilter<ConsumeContext<TMessage>>
        where TMessage : class
    {
        public async Task Send(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
        {
            var processId = await processTrackingStore.GetOrCreateProcessAsync(
                typeof(TMessage).Name,
                context.CorrelationId ?? context.MessageId,
                context.ConversationId,
                context.MessageId,
                context.CancellationToken);

            var stepId = await processTrackingStore.StartStepAsync(
                processId,
                $"{typeof(TMessage).Name}.Consume",
                ProcessStepComponentType.Consumer,
                0,
                context.CancellationToken);

            try
            {
                await next.Send(context);
                await processTrackingStore.CompleteStepAsync(processId, stepId, context.CancellationToken);
            }
            catch (Exception exception)
            {
                await processTrackingStore.FailStepAsync(
                    processId,
                    stepId,
                    exception.Message,
                    context.CancellationToken);
                throw;
            }
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope(nameof(ProcessTrackingConsumeFilter<TMessage>));
        }
    }
}
