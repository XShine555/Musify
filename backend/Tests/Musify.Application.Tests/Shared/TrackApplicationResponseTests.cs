using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Shared
{
    public sealed class TrackApplicationResponseTests
    {
        [Fact]
        public void FromEntity_LocalTrack_UsesOwnerNameAsArtist()
        {
            var owner = TestEntities.User(1, "The Owner");
            var track = TestEntities.LocalTrack(owner);

            var response = Assert.IsType<LocalTrackApplicationResponse>(TrackApplicationResponse.FromEntity(track, listensCount: 3));

            Assert.Equal("The Owner", response.Artist);
            Assert.Equal(owner.Id, response.OwnerUserId);
            Assert.Equal(3, response.ListensCount);
        }

        [Fact]
        public void FromEntity_ExternalTrackWithArtists_JoinsThemInPositionOrder()
        {
            var track = TestEntities.ExternalTrack();
            var second = TestEntities.Artist("Second Artist", externalId: "second");
            var first = TestEntities.Artist("First Artist", externalId: "first");
            track.TrackArtists =
            [
                new TrackArtist { TrackId = track.Id, ArtistId = second.Id, Position = 1, Artist = second, Track = track },
                new TrackArtist { TrackId = track.Id, ArtistId = first.Id, Position = 0, Artist = first, Track = track }
            ];

            var response = Assert.IsType<ExternalTrackApplicationResponse>(TrackApplicationResponse.FromEntity(track, listensCount: 0));

            Assert.Equal("First Artist, Second Artist", response.Artist);
            Assert.Equal(track.ExternalId, response.ExternalId);
        }

        [Fact]
        public void FromEntity_ExternalTrackWithNoArtists_HasNullArtist()
        {
            var track = TestEntities.ExternalTrack();

            var response = TrackApplicationResponse.FromEntity(track, listensCount: 0);

            Assert.Null(response.Artist);
        }
    }
}
