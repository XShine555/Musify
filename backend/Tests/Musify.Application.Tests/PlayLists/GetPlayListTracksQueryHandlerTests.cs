using ErrorOr;
using Musify.Application.PlayLists;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.PlayLists
{
    public sealed class GetPlayListTracksQueryHandlerTests : HandlerTestBase
    {
        private GetPlayListTracksQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_TracksOrderedByPosition_ReturnsThemInOrder()
        {
            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id);
            var second = TestEntities.Track(owner, "Second");
            var first = TestEntities.Track(owner, "First");
            await SeedAsync(
                owner, playList, second, first,
                new PlayListHasTrack { PlayListId = playList.Id, TrackId = second.Id, Position = 1 },
                new PlayListHasTrack { PlayListId = playList.Id, TrackId = first.Id, Position = 0 });

            var result = await CreateHandler().Handle(new GetPlayListTracksQuery(playList.Id, PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(["First", "Second"], result.Value.Items.Select(track => track.Title));
        }

        [Fact]
        public async Task Handle_PlayListMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new GetPlayListTracksQuery(Guid.NewGuid(), PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
