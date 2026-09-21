using ErrorOr;
using Musify.Application.PlayLists;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.PlayLists
{
    public sealed class GetPlayListByIdQueryHandlerTests : HandlerTestBase
    {
        private GetPlayListByIdQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_ExistingPlayList_ReturnsItWithCoverTrackIds()
        {
            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id, visibility: Musify.Domain.ValueObjects.PlaylistVisibility.Public);
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, playList, track, new PlayListHasTrack { PlayListId = playList.Id, TrackId = track.Id, Position = 0 });

            var result = await CreateHandler().Handle(new GetPlayListByIdQuery(playList.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(playList.Name, result.Value.Name);
            Assert.Equal([track.Id], result.Value.CoverTrackIds);
        }

        [Fact]
        public async Task Handle_PlayListMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new GetPlayListByIdQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_PrivatePlayList_RequestedByOwner_ReturnsIt()
        {
            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id, visibility: Musify.Domain.ValueObjects.PlaylistVisibility.Private);
            await SeedAsync(owner, playList);

            var result = await CreateHandler().Handle(new GetPlayListByIdQuery(playList.Id, owner.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
        }

        [Fact]
        public async Task Handle_PrivatePlayList_RequestedByStranger_ReturnsNotFound()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var playList = TestEntities.PlayList(owner.Id, visibility: Musify.Domain.ValueObjects.PlaylistVisibility.Private);
            await SeedAsync(owner, stranger, playList);

            var result = await CreateHandler().Handle(new GetPlayListByIdQuery(playList.Id, stranger.Id), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_PrivatePlayList_RequestedAnonymously_ReturnsNotFound()
        {
            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id, visibility: Musify.Domain.ValueObjects.PlaylistVisibility.Private);
            await SeedAsync(owner, playList);

            var result = await CreateHandler().Handle(new GetPlayListByIdQuery(playList.Id, RequestingUserId: null), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_PublicPlayList_RequestedAnonymously_ReturnsIt()
        {
            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id, visibility: Musify.Domain.ValueObjects.PlaylistVisibility.Public);
            await SeedAsync(owner, playList);

            var result = await CreateHandler().Handle(new GetPlayListByIdQuery(playList.Id, RequestingUserId: null), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
        }
    }
}
