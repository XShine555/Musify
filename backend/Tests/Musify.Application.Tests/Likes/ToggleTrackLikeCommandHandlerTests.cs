using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Likes;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Likes
{
    public sealed class ToggleTrackLikeCommandHandlerTests : HandlerTestBase
    {
        private readonly IYouTubeMusicService youTubeMusicService = Substitute.For<IYouTubeMusicService>();

        private ToggleTrackLikeCommandHandler CreateHandler()
        {
            var provisioner = new YouTubeTrackProvisioner(
                Database, youTubeMusicService, TestConfigurations.Track(), NoOpLogger<YouTubeTrackProvisioner>());

            return new ToggleTrackLikeCommandHandler(Database, provisioner, NoOpLogger<ToggleTrackLikeCommandHandler>());
        }

        private static YouTubeSongResult BuildSong(string videoId) => new(
            videoId, "Song Title", "Song Artist", "Song Album", 200, "https://img.example/thumb.jpg", false,
            [new YouTubeArtistRef("artist-id", "Song Artist")]);

        [Fact]
        public async Task Handle_LocalTrackNotLiked_LikesIt()
        {
            var owner = TestEntities.User();
            var track = TestEntities.LocalTrack(owner);
            await SeedAsync(owner, track);

            var result = await CreateHandler().Handle(
                new ToggleTrackLikeCommand(owner.Id, track.Id, null, null), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.True(result.Value);
            Assert.Single(await Database.TrackLikes.ToListAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Handle_LocalTrackAlreadyLiked_UnlikesIt()
        {
            var owner = TestEntities.User();
            var track = TestEntities.LocalTrack(owner);
            await SeedAsync(owner, track, new Musify.Domain.Entities.TrackLike { UserId = owner.Id, TrackId = track.Id });

            var result = await CreateHandler().Handle(
                new ToggleTrackLikeCommand(owner.Id, track.Id, null, null), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.False(result.Value);
            Assert.Empty(await Database.TrackLikes.ToListAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Handle_TrackMissing_ReturnsNotFound()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner);

            var result = await CreateHandler().Handle(
                new ToggleTrackLikeCommand(owner.Id, Guid.NewGuid(), null, null), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_UnknownExternalTrack_ProvisionsAndLikesIt()
        {
            youTubeMusicService.GetSongAsync("new-video", Arg.Any<CancellationToken>()).Returns(BuildSong("new-video"));

            var owner = TestEntities.User();
            await SeedAsync(owner);

            var result = await CreateHandler().Handle(
                new ToggleTrackLikeCommand(owner.Id, null, TrackSource.YouTube, "new-video"), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.True(result.Value);
            var track = await Database.ExternalTracks.SingleAsync(t => t.ExternalId == "new-video", TestContext.Current.CancellationToken);
            Assert.True(await Database.TrackLikes.AnyAsync(like => like.TrackId == track.Id, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Handle_ExistingExternalTrack_LikesItWithoutReprovisioning()
        {
            var owner = TestEntities.User();
            var track = TestEntities.ExternalTrack(externalId: "abc123");
            await SeedAsync(owner, track);

            var result = await CreateHandler().Handle(
                new ToggleTrackLikeCommand(owner.Id, null, TrackSource.YouTube, "abc123"), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.True(result.Value);
            await youTubeMusicService.DidNotReceive().GetSongAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }
}
