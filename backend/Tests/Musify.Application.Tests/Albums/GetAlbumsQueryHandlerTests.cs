using ErrorOr;
using Musify.Application.Albums;
using Musify.Application.Contracts;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class GetAlbumsQueryHandlerTests : HandlerTestBase
    {
        private readonly IYouTubeMusicService youTubeMusicService = Substitute.For<IYouTubeMusicService>();

        private GetAlbumsQueryHandler CreateHandler() =>
            new(Database, youTubeMusicService, NoOpLogger<GetAlbumsQueryHandler>());

        [Fact]
        public async Task Handle_NoTitleFilter_ReturnsAllAlbumsPaged_AndDoesNotCallYouTube()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner, TestEntities.UserAlbum(owner.Id, "Alpha"), TestEntities.UserAlbum(owner.Id, "Beta"));

            var result = await CreateHandler().Handle(new GetAlbumsQuery(Title: null, PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.TotalItemCount);
            Assert.All(result.Value.Items, item => Assert.NotNull(item.Album));
            await youTubeMusicService.DidNotReceiveWithAnyArgs()
                .SearchAlbumsAsync(default!, default!, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Handle_TitleFilter_ReturnsMatchingLocalAlbums_FollowedByYouTubeResults()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner, TestEntities.UserAlbum(owner.Id, "Rock Classics"), TestEntities.UserAlbum(owner.Id, "Jazz Favorites"));

            var album = new YouTubeAlbumResult("album-1", "Rock Classics Reissue", "Various Artists", "thumb.jpg", 2020, false, false, []);
            youTubeMusicService.SearchAlbumsAsync("rock", string.Empty, Arg.Any<CancellationToken>())
                .Returns(new YouTubeAlbumSearchResult([album], "next-token"));

            var result = await CreateHandler().Handle(new GetAlbumsQuery(Title: "rock", PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.Items.Count);
            Assert.NotNull(result.Value.Items[0].Album);
            Assert.Equal("Rock Classics", result.Value.Items[0].Album!.Title);
            Assert.Null(result.Value.Items[1].Album);
            Assert.NotNull(result.Value.Items[1].YouTubeAlbum);
            Assert.Equal("album-1", result.Value.Items[1].YouTubeAlbum!.AlbumId);
            Assert.Equal("next-token", result.Value.NextYoutubeContinuationToken);
            Assert.False(result.Value.YoutubeUnavailable);
        }

        [Fact]
        public async Task Handle_TitleFilter_SkipsYouTubeResultAlreadyMaterializedLocally()
        {
            var externalAlbum = TestEntities.ExternalAlbum(externalId: "album-1", title: "Rock Classics", source: TrackSource.YouTube);
            await SeedAsync(externalAlbum);

            var album = new YouTubeAlbumResult("album-1", "Rock Classics", "Various Artists", "thumb.jpg", 2020, false, false, []);
            youTubeMusicService.SearchAlbumsAsync("rock", string.Empty, Arg.Any<CancellationToken>())
                .Returns(new YouTubeAlbumSearchResult([album], string.Empty));

            var result = await CreateHandler().Handle(new GetAlbumsQuery(Title: "rock", PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var item = Assert.Single(result.Value.Items);
            Assert.NotNull(item.Album);
            Assert.Null(item.YouTubeAlbum);
        }

        [Fact]
        public async Task Handle_TitleFilter_YouTubeUnavailable_StillReturnsLocalResults()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner, TestEntities.UserAlbum(owner.Id, "Rock Classics"));

            youTubeMusicService.SearchAlbumsAsync("rock", string.Empty, Arg.Any<CancellationToken>())
                .Returns(Error.Forbidden(description: "The YouTube Music source is disabled."));

            var result = await CreateHandler().Handle(new GetAlbumsQuery(Title: "rock", PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var item = Assert.Single(result.Value.Items);
            Assert.NotNull(item.Album);
            Assert.True(result.Value.YoutubeUnavailable);
            Assert.Null(result.Value.NextYoutubeContinuationToken);
        }

        [Fact]
        public async Task Handle_PageSizeSmallerThanTotal_ReportsHasNextPage()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner, TestEntities.UserAlbum(owner.Id, "One"), TestEntities.UserAlbum(owner.Id, "Two"), TestEntities.UserAlbum(owner.Id, "Three"));

            var result = await CreateHandler().Handle(new GetAlbumsQuery(Title: null, PageNumber: 1, PageSize: 2), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.Items.Count);
            Assert.True(result.Value.HasNextPage);
        }
    }
}
