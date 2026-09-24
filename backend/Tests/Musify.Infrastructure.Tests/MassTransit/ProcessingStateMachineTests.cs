using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Events;
using Musify.Application.Shared;
using Musify.Infrastructure.MassTransit.Sagas;
using Xunit;

namespace Musify.Infrastructure.Tests.MassTransit;

public sealed class ProcessingStateMachineTests
{
    private static readonly ImageSizes Sizes = new(new ImageSize("s", 1, 1), new ImageSize("m", 2, 2), new ImageSize("l", 3, 3));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Track

    [Fact]
    public async Task Track_PictureAndAudioProcessed_CompletesTheSaga()
    {
        await using var provider = CreateProvider<TrackProcessingStateMachine, TrackProcessingState>();
        var harness = await StartAsync(provider);
        var saga = provider.GetRequiredService<ISagaStateMachineTestHarness<TrackProcessingStateMachine, TrackProcessingState>>();
        var id = Guid.NewGuid();

        await harness.Bus.Publish(TrackStarted(id), Ct);
        Assert.NotNull(await saga.Exists(id, machine => machine.Processing));

        await harness.Bus.Publish(new TrackPictureProcessed(id), Ct);
        await harness.Bus.Publish(new TrackAudioProcessed(id), Ct);

        Assert.Null(await saga.NotExists(id));
        Assert.False(await harness.Published.Any<TrackProcessingFailed>(Ct));
    }

    [Fact]
    public async Task Track_PictureFailed_PublishesProcessingFailedAndMovesToFailed()
    {
        await using var provider = CreateProvider<TrackProcessingStateMachine, TrackProcessingState>();
        var harness = await StartAsync(provider);
        var saga = provider.GetRequiredService<ISagaStateMachineTestHarness<TrackProcessingStateMachine, TrackProcessingState>>();
        var id = Guid.NewGuid();

        await harness.Bus.Publish(TrackStarted(id), Ct);
        Assert.NotNull(await saga.Exists(id, machine => machine.Processing));

        await harness.Bus.Publish(new TrackPictureProcessingFailed(id), Ct);

        Assert.NotNull(await saga.Exists(id, machine => machine.Failed));
        Assert.True(await harness.Published.Any<TrackProcessingFailed>(
            published => published.Context.Message is { Bucket: "bucket", PictureKey: "picture", AudioKey: "audio" }
                && published.Context.Message.TrackId == id, Ct));
    }

    [Fact]
    public async Task Track_AudioFailed_PublishesProcessingFailed()
    {
        await using var provider = CreateProvider<TrackProcessingStateMachine, TrackProcessingState>();
        var harness = await StartAsync(provider);
        var saga = provider.GetRequiredService<ISagaStateMachineTestHarness<TrackProcessingStateMachine, TrackProcessingState>>();
        var id = Guid.NewGuid();

        await harness.Bus.Publish(TrackStarted(id), Ct);
        Assert.NotNull(await saga.Exists(id, machine => machine.Processing));

        await harness.Bus.Publish(new TrackAudioProcessingFailed(id), Ct);

        Assert.NotNull(await saga.Exists(id, machine => machine.Failed));
        Assert.True(await harness.Published.Any<TrackProcessingFailed>(Ct));
    }

    [Fact]
    public async Task Track_EventForMissingInstance_IsDiscarded()
    {
        await using var provider = CreateProvider<TrackProcessingStateMachine, TrackProcessingState>();
        var harness = await StartAsync(provider);
        var id = Guid.NewGuid();

        await harness.Bus.Publish(new TrackPictureProcessed(id), Ct);
        await harness.Bus.Publish(new TrackAudioProcessingFailed(id), Ct);

        Assert.True(await harness.Consumed.Any<TrackAudioProcessingFailed>(Ct));
        Assert.False(await harness.Published.Any<Fault<TrackPictureProcessed>>(Ct));
        Assert.False(await harness.Published.Any<Fault<TrackAudioProcessingFailed>>(Ct));
        Assert.False(await harness.Published.Any<TrackProcessingFailed>(Ct));
    }

    // Album

    [Fact]
    public async Task Album_PictureProcessed_CompletesTheSaga()
    {
        await using var provider = CreateProvider<AlbumProcessingStateMachine, AlbumProcessingState>();
        var harness = await StartAsync(provider);
        var saga = provider.GetRequiredService<ISagaStateMachineTestHarness<AlbumProcessingStateMachine, AlbumProcessingState>>();
        var id = Guid.NewGuid();

        await harness.Bus.Publish(new CreateAlbumResourcesEvent(id, Guid.NewGuid(), "bucket", "source", "picture", Sizes), Ct);
        Assert.NotNull(await saga.Exists(id, machine => machine.Processing));

        await harness.Bus.Publish(new AlbumPictureProcessed(id), Ct);

        Assert.Null(await saga.NotExists(id));
        Assert.False(await harness.Published.Any<AlbumProcessingFailed>(Ct));
    }

    [Fact]
    public async Task Album_PictureFailed_PublishesProcessingFailed()
    {
        await using var provider = CreateProvider<AlbumProcessingStateMachine, AlbumProcessingState>();
        var harness = await StartAsync(provider);
        var saga = provider.GetRequiredService<ISagaStateMachineTestHarness<AlbumProcessingStateMachine, AlbumProcessingState>>();
        var id = Guid.NewGuid();

        await harness.Bus.Publish(new CreateAlbumResourcesEvent(id, Guid.NewGuid(), "bucket", "source", "picture", Sizes), Ct);
        Assert.NotNull(await saga.Exists(id, machine => machine.Processing));

        await harness.Bus.Publish(new AlbumPictureProcessingFailed(id), Ct);

        Assert.NotNull(await saga.Exists(id, machine => machine.Failed));
        Assert.True(await harness.Published.Any<AlbumProcessingFailed>(
            published => published.Context.Message is { Bucket: "bucket", PictureKey: "picture" }
                && published.Context.Message.AlbumId == id, Ct));
    }

    [Fact]
    public async Task Album_EventForMissingInstance_IsDiscarded()
    {
        await using var provider = CreateProvider<AlbumProcessingStateMachine, AlbumProcessingState>();
        var harness = await StartAsync(provider);

        await harness.Bus.Publish(new AlbumPictureProcessingFailed(Guid.NewGuid()), Ct);

        Assert.True(await harness.Consumed.Any<AlbumPictureProcessingFailed>(Ct));
        Assert.False(await harness.Published.Any<AlbumProcessingFailed>(Ct));
        Assert.False(await harness.Published.Any<Fault<AlbumPictureProcessingFailed>>(Ct));
    }

    // PlayList

    [Fact]
    public async Task PlayList_PictureProcessed_CompletesTheSaga()
    {
        await using var provider = CreateProvider<PlayListProcessingStateMachine, PlayListProcessingState>();
        var harness = await StartAsync(provider);
        var saga = provider.GetRequiredService<ISagaStateMachineTestHarness<PlayListProcessingStateMachine, PlayListProcessingState>>();
        var id = Guid.NewGuid();

        await harness.Bus.Publish(new CreatePlayListResourcesEvent(id, Guid.NewGuid(), "bucket", "source", "picture", Sizes), Ct);
        Assert.NotNull(await saga.Exists(id, machine => machine.Processing));

        await harness.Bus.Publish(new PlayListPictureProcessed(id), Ct);

        Assert.Null(await saga.NotExists(id));
        Assert.False(await harness.Published.Any<PlayListProcessingFailed>(Ct));
    }

    [Fact]
    public async Task PlayList_PictureFailed_PublishesProcessingFailed()
    {
        await using var provider = CreateProvider<PlayListProcessingStateMachine, PlayListProcessingState>();
        var harness = await StartAsync(provider);
        var saga = provider.GetRequiredService<ISagaStateMachineTestHarness<PlayListProcessingStateMachine, PlayListProcessingState>>();
        var id = Guid.NewGuid();

        await harness.Bus.Publish(new CreatePlayListResourcesEvent(id, Guid.NewGuid(), "bucket", "source", "picture", Sizes), Ct);
        Assert.NotNull(await saga.Exists(id, machine => machine.Processing));

        await harness.Bus.Publish(new PlayListPictureProcessingFailed(id), Ct);

        Assert.NotNull(await saga.Exists(id, machine => machine.Failed));
        Assert.True(await harness.Published.Any<PlayListProcessingFailed>(
            published => published.Context.Message is { Bucket: "bucket", PictureKey: "picture" }
                && published.Context.Message.PlayListId == id, Ct));
    }

    [Fact]
    public async Task PlayList_EventForMissingInstance_IsDiscarded()
    {
        await using var provider = CreateProvider<PlayListProcessingStateMachine, PlayListProcessingState>();
        var harness = await StartAsync(provider);

        await harness.Bus.Publish(new PlayListPictureProcessingFailed(Guid.NewGuid()), Ct);

        Assert.True(await harness.Consumed.Any<PlayListPictureProcessingFailed>(Ct));
        Assert.False(await harness.Published.Any<PlayListProcessingFailed>(Ct));
        Assert.False(await harness.Published.Any<Fault<PlayListPictureProcessingFailed>>(Ct));
    }

    private static CreateTrackResourcesEvent TrackStarted(Guid id) =>
        new(id, Guid.NewGuid(), Guid.NewGuid(), "bucket", "picture-source", "picture", "audio-source", "audio", "audio-folder", Sizes);

    private static async Task<ITestHarness> StartAsync(ServiceProvider provider)
    {
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        return harness;
    }

    private static ServiceProvider CreateProvider<TMachine, TState>()
        where TMachine : class, SagaStateMachine<TState>
        where TState : class, SagaStateMachineInstance =>
        new ServiceCollection()
            .AddMassTransitTestHarness(bus => bus.AddSagaStateMachine<TMachine, TState>().InMemoryRepository())
            .BuildServiceProvider(true);
}
