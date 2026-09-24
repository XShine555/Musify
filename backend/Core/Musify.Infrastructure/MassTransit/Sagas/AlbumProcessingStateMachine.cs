using MassTransit;
using Musify.Application.Events;

namespace Musify.Infrastructure.MassTransit.Sagas;

public sealed class AlbumProcessingStateMachine : MassTransitStateMachine<AlbumProcessingState>
{
    public State Processing { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<CreateAlbumResourcesEvent> ProcessingStarted { get; private set; } = null!;
    public Event<AlbumPictureProcessed> PictureProcessed { get; private set; } = null!;
    public Event<AlbumPictureProcessingFailed> PictureFailed { get; private set; } = null!;

    public AlbumProcessingStateMachine()
    {
        InstanceState(state => state.CurrentState);

        Event(() => ProcessingStarted, config => config.CorrelateById(context => context.Message.AlbumId));
        Event(() => PictureProcessed, config => config.CorrelateById(context => context.Message.AlbumId));
        Event(() => PictureFailed, config => config.CorrelateById(context => context.Message.AlbumId));

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
                .Publish(context => new AlbumProcessingFailed(
                    context.Saga.CorrelationId,
                    context.Saga.Bucket,
                    context.Saga.PictureKey)));

        During(Failed,
            Ignore(PictureProcessed),
            Ignore(PictureFailed));

        SetCompletedWhenFinalized();
    }
}
