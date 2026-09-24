using MassTransit;
using Musify.Application.Events;

namespace Musify.Infrastructure.MassTransit.Sagas;

public sealed class TrackProcessingStateMachine : MassTransitStateMachine<TrackProcessingState>
{
    public State Processing { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<CreateTrackResourcesEvent> ProcessingStarted { get; private set; } = null!;
    public Event<TrackPictureProcessed> PictureProcessed { get; private set; } = null!;
    public Event<TrackAudioProcessed> AudioProcessed { get; private set; } = null!;
    public Event<TrackPictureProcessingFailed> PictureFailed { get; private set; } = null!;
    public Event<TrackAudioProcessingFailed> AudioFailed { get; private set; } = null!;

    public TrackProcessingStateMachine()
    {
        InstanceState(state => state.CurrentState);

        Event(() => ProcessingStarted, config => config.CorrelateById(context => context.Message.TrackId));
        Event(() => PictureProcessed, config => config.CorrelateById(context => context.Message.TrackId));
        Event(() => AudioProcessed, config => config.CorrelateById(context => context.Message.TrackId));
        Event(() => PictureFailed, config => config.CorrelateById(context => context.Message.TrackId));
        Event(() => AudioFailed, config => config.CorrelateById(context => context.Message.TrackId));

        Initially(
            When(ProcessingStarted)
                .Then(context =>
                {
                    context.Saga.CreatedAt = DateTime.UtcNow;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    context.Saga.Bucket = context.Message.Bucket;
                    context.Saga.PictureKey = context.Message.PictureDestinationKey;
                    context.Saga.AudioKey = context.Message.AudioDestinationKey;
                })
                .TransitionTo(Processing));

        During(Processing,
            When(PictureProcessed)
                .Then(context =>
                {
                    context.Saga.PictureProcessed = true;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .If(context => context.Saga.AudioProcessed, binder => binder.Finalize()),
            When(AudioProcessed)
                .Then(context =>
                {
                    context.Saga.AudioProcessed = true;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .If(context => context.Saga.PictureProcessed, binder => binder.Finalize()),
            When(PictureFailed)
                .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                .TransitionTo(Failed)
                .Publish(context => new TrackProcessingFailed(
                    context.Saga.CorrelationId,
                    context.Saga.Bucket,
                    context.Saga.PictureKey,
                    context.Saga.AudioKey)),
            When(AudioFailed)
                .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                .TransitionTo(Failed)
                .Publish(context => new TrackProcessingFailed(
                    context.Saga.CorrelationId,
                    context.Saga.Bucket,
                    context.Saga.PictureKey,
                    context.Saga.AudioKey)));

        During(Failed,
            Ignore(PictureProcessed),
            Ignore(AudioProcessed),
            Ignore(PictureFailed),
            Ignore(AudioFailed));

        SetCompletedWhenFinalized();
    }
}
