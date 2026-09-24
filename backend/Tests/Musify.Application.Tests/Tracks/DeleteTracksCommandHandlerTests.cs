using ErrorOr;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class DeleteTracksCommandHandlerTests : HandlerTestBase
    {
        private readonly IEventBus eventBus = Substitute.For<IEventBus>();

        private DeleteTracksCommandHandler CreateHandler() => new(Database, eventBus, NoOpLogger<DeleteTracksCommandHandler>());

        [Fact]
        public async Task Handle_OwnTracks_MarksAllAsRemovingAndPublishesOneEventEach()
        {
            var owner = TestEntities.User();
            var first = TestEntities.Track(owner, "One");
            var second = TestEntities.Track(owner, "Two");
            await SeedAsync(owner, first, second);

            var result = await CreateHandler().Handle(new DeleteTracksCommand(owner.Id, [first.Id, second.Id]), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.DeletedTrackIds.Count);
            Assert.Empty(result.Value.FailedTrackIds);
            Assert.All(Database.Tracks.ToList(), track => Assert.Equal(LifeCycleStatus.Removing, track.LifeCycleStatus));
            await eventBus.Received(2).PublishAsync(Arg.Any<DeleteTrackEvent>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_TrackOfAnotherUser_ReturnsForbiddenAndDeletesNothing()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var mine = TestEntities.Track(owner, "Mine");
            var theirs = TestEntities.Track(stranger, "Theirs");
            await SeedAsync(owner, stranger, mine, theirs);

            var result = await CreateHandler().Handle(new DeleteTracksCommand(owner.Id, [mine.Id, theirs.Id]), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
            Assert.All(Database.Tracks.ToList(), track => Assert.Equal(LifeCycleStatus.Active, track.LifeCycleStatus));
            await eventBus.DidNotReceive().PublishAsync(Arg.Any<DeleteTrackEvent>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ProcessingAndMissingTracks_AreReturnedAsFailed()
        {
            var owner = TestEntities.User();
            var done = TestEntities.Track(owner, "Done");
            var busy = TestEntities.Track(owner, "Busy", audio: TestEntities.PendingAudio());
            var missing = Guid.NewGuid();
            await SeedAsync(owner, done, busy);

            var result = await CreateHandler().Handle(new DeleteTracksCommand(owner.Id, [done.Id, busy.Id, missing]), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal([done.Id], result.Value.DeletedTrackIds);
            Assert.Equal([busy.Id, missing], result.Value.FailedTrackIds);
            Assert.Equal(LifeCycleStatus.Active, Database.Tracks.Single(t => t.Id == busy.Id).LifeCycleStatus);
        }

        [Fact]
        public async Task Handle_DuplicateIds_AreDeletedOnce()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, track);

            var result = await CreateHandler().Handle(new DeleteTracksCommand(owner.Id, [track.Id, track.Id]), TestContext.Current.CancellationToken);

            Assert.Single(result.Value.DeletedTrackIds);
            await eventBus.Received(1).PublishAsync(Arg.Any<DeleteTrackEvent>(), Arg.Any<CancellationToken>());
        }
    }
}
