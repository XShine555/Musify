using ErrorOr;
using Musify.Application.Contracts;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class GetTracksQueryHandlerTests : HandlerTestBase
    {
        private readonly IYouTubeMusicService youTubeMusicService = Substitute.For<IYouTubeMusicService>();

        private GetTracksQueryHandler CreateHandler() =>
            new(Database, youTubeMusicService, NoOpLogger<GetTracksQueryHandler>());

        [Fact]
        public async Task Handle_NoNameFilter_ReturnsEveryTrackAUserOwns_AndDoesNotCallYouTube()
        {
            var owner = TestEntities.User();
            var trackA = TestEntities.LocalTrack(owner, "Alpha");
            var trackB = TestEntities.LocalTrack(owner, "Beta");
            await SeedAsync(
                owner, trackA, trackB,
                new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = trackA.Id },
                new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = trackB.Id });

            var result = await CreateHandler().Handle(new GetTracksQuery(Name: null, PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.TotalItemCount);
            Assert.Equal(2, result.Value.Items.Count);
            Assert.All(result.Value.Items, item => Assert.NotNull(item.Track));
            await youTubeMusicService.DidNotReceiveWithAnyArgs()
                .SearchSongsAsync(default!, default!, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Handle_NameFilter_ReturnsMatchingLocalTracks_FollowedByYouTubeResults()
        {
            var owner = TestEntities.User();
            var match = TestEntities.LocalTrack(owner, "Bohemian Rhapsody");
            var other = TestEntities.LocalTrack(owner, "Imagine");
            await SeedAsync(
                owner, match, other,
                new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = match.Id },
                new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = other.Id });

            var song = new YouTubeSongResult("video-1", "Bohemian Rhapsody (Live)", "Queen", "A Night at the Opera",
                354, "thumb.jpg", false, []);
            youTubeMusicService.SearchSongsAsync("rhapsody", string.Empty, Arg.Any<CancellationToken>())
                .Returns(new YouTubeSearchResult([song], "next-token"));

            var result = await CreateHandler().Handle(new GetTracksQuery(Name: "rhapsody", PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.Items.Count);
            Assert.NotNull(result.Value.Items[0].Track);
            Assert.Equal("Bohemian Rhapsody", result.Value.Items[0].Track!.Title);
            Assert.Null(result.Value.Items[1].Track);
            Assert.NotNull(result.Value.Items[1].YouTubeSong);
            Assert.Equal("video-1", result.Value.Items[1].YouTubeSong!.VideoId);
            Assert.Equal("next-token", result.Value.NextYoutubeContinuationToken);
            Assert.False(result.Value.YoutubeUnavailable);
        }

        [Fact]
        public async Task Handle_NameFilter_SkipsYouTubeResultAlreadyProvisionedLocally()
        {
            var owner = TestEntities.User();
            var provisioned = TestEntities.ExternalTrack(externalId: "video-1", title: "Bohemian Rhapsody", source: TrackSource.YouTube);
            await SeedAsync(
                owner, provisioned,
                new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = provisioned.Id });

            var song = new YouTubeSongResult("video-1", "Bohemian Rhapsody", "Queen", "A Night at the Opera",
                354, "thumb.jpg", false, []);
            youTubeMusicService.SearchSongsAsync("rhapsody", string.Empty, Arg.Any<CancellationToken>())
                .Returns(new YouTubeSearchResult([song], string.Empty));

            var result = await CreateHandler().Handle(new GetTracksQuery(Name: "rhapsody", PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var item = Assert.Single(result.Value.Items);
            Assert.NotNull(item.Track);
            Assert.Null(item.YouTubeSong);
        }

        [Fact]
        public async Task Handle_NameFilter_YouTubeUnavailable_StillReturnsLocalResults()
        {
            var owner = TestEntities.User();
            var match = TestEntities.LocalTrack(owner, "Bohemian Rhapsody");
            await SeedAsync(
                owner, match,
                new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = match.Id });

            youTubeMusicService.SearchSongsAsync("rhapsody", string.Empty, Arg.Any<CancellationToken>())
                .Returns(Error.Forbidden(description: "The YouTube Music source is disabled."));

            var result = await CreateHandler().Handle(new GetTracksQuery(Name: "rhapsody", PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var item = Assert.Single(result.Value.Items);
            Assert.NotNull(item.Track);
            Assert.True(result.Value.YoutubeUnavailable);
            Assert.Null(result.Value.NextYoutubeContinuationToken);
        }
    }
}
