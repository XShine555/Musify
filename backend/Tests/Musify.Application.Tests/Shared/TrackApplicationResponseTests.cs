using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks.Responses;
using Xunit;

namespace Musify.Application.Tests.Shared
{
    public sealed class TrackApplicationResponseTests
    {
        [Fact]
        public void FromEntity_UsesOwnerNameAsArtist()
        {
            var owner = TestEntities.User(1, "The Owner");
            var track = TestEntities.Track(owner);

            var response = TrackApplicationResponse.FromEntity(track, listensCount: 3);

            Assert.Equal("The Owner", response.Artist);
            Assert.Equal(owner.Id, response.OwnerUserId);
            Assert.Equal(3, response.ListensCount);
        }
    }
}
