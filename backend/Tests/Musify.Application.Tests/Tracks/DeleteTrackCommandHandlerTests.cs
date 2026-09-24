using ErrorOr;
using Musify.Application.Contracts;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Tracks;

public sealed class DeleteTrackCommandHandlerTests : HandlerTestBase
{
    private readonly IEventBus eventBus = Substitute.For<IEventBus>();

    private DeleteTrackCommandHandler CreateHandler() => new(Database, eventBus, NoOpLogger<DeleteTrackCommandHandler>());

    [Fact]
    public async Task Handle_Owner_MarksTrackAsRemovingAndPublishesEvent()
    {
        var owner = TestEntities.User();
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, track);

        var result = await CreateHandler().Handle(new DeleteTrackCommand(owner.Id, track.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var stored = await Database.Tracks.FindAsync([track.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
        Assert.Equal(LifeCycleStatus.Removing, stored.LifeCycleStatus);
        await eventBus.Received(1).PublishAsync(Arg.Any<Musify.Application.Events.DeleteTrackEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TrackMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new DeleteTrackCommand(1, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsUnauthorized()
    {
        var owner = TestEntities.User(1, "owner");
        var stranger = TestEntities.User(2, "stranger");
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, stranger, track);

        var result = await CreateHandler().Handle(new DeleteTrackCommand(stranger.Id, track.Id), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_AudioStillProcessing_ReturnsConflict()
    {
        var owner = TestEntities.User();
        var track = TestEntities.Track(owner, audio: TestEntities.PendingAudio());
        await SeedAsync(owner, track);

        var result = await CreateHandler().Handle(new DeleteTrackCommand(owner.Id, track.Id), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
    }
}
