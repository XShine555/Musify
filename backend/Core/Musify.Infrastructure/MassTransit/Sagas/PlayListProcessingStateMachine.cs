using MassTransit;
using Musify.Application.Events;

namespace Musify.Infrastructure.MassTransit.Sagas;

public sealed class PlayListProcessingStateMachine : MassTransitStateMachine<PlayListProcessingState>
{
    public State Processing { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<CreatePlayListResourcesEvent> ProcessingStarted { get; private set; } = null!;
    public Event<PlayListPictureProcessed> PictureProcessed { get; private set; } = null!;
    public Event<PlayListPictureProcessingFailed> PictureFailed { get; private set; } = null!;

    public PlayListProcessingStateMachine()
    {
        InstanceState(state => state.CurrentState);

        Event(() => ProcessingStarted, config => config.CorrelateById(context => context.Message.PlayListId));
        Event(() => PictureProcessed, config =>
        {
            config.CorrelateById(context => context.Message.PlayListId);
            config.OnMissingInstance(instance => instance.Discard());
        });
        Event(() => PictureFailed, config =>
        {
            config.CorrelateById(context => context.Message.PlayListId);
            config.OnMissingInstance(instance => instance.Discard());
        });

        Initially(
            When(ProcessingStarted)
                .Then(context =>
                {
                    context.Saga.CreatedAt = DateTime.UtcNow;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    context.Saga.Bucket = context.Message.Bucket;
                    context.Saga.PictureKey = context.Message.PictureDestinationKey;
                })
                .TransitionTo(Processing));

        During(Processing,
            When(PictureProcessed).Finalize(),
            When(PictureFailed)
                .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                .TransitionTo(Failed)
                .Publish(context => new PlayListProcessingFailed(
                    context.Saga.CorrelationId,
                    context.Saga.Bucket,
                    context.Saga.PictureKey)));

        During(Failed,
            Ignore(PictureProcessed),
            Ignore(PictureFailed));

        SetCompletedWhenFinalized();
    }
}
