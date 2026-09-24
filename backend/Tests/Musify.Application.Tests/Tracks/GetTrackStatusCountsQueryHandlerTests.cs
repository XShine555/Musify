using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class GetTrackStatusCountsQueryHandlerTests : HandlerTestBase
    {
        [Fact]
        public async Task Handle_CountsOnlyActiveTracksOfTheUser()
        {
            var owner = TestEntities.User(1, "owner");
            var other = TestEntities.User(2, "other");
            await SeedAsync(
                owner, other,
                TestEntities.Track(owner, "Done 1"),
                TestEntities.Track(owner, "Done 2"),
                TestEntities.Track(owner, "Pending", audio: TestEntities.PendingAudio()),
                TestEntities.Track(owner, "Processing", audio: new TrackAudio { TranscodeStatus = ProcessingStatus.Processing }),
                TestEntities.Track(owner, "Failed", audio: new TrackAudio { TranscodeStatus = ProcessingStatus.Failed }),
                TestEntities.Track(owner, "Removing", lifeCycleStatus: LifeCycleStatus.Removing),
                TestEntities.Track(other, "Theirs"));

            var counts = await new GetTrackStatusCountsQueryHandler(Database)
                .Handle(new GetTrackStatusCountsQuery(owner.Id), TestContext.Current.CancellationToken);

            Assert.Equal(5, counts.Total);
            Assert.Equal(1, counts.Pending);
            Assert.Equal(1, counts.Processing);
            Assert.Equal(2, counts.Completed);
            Assert.Equal(1, counts.Failed);
        }

        [Fact]
        public async Task Handle_NoTracks_ReturnsZeros()
        {
            var counts = await new GetTrackStatusCountsQueryHandler(Database)
                .Handle(new GetTrackStatusCountsQuery(99), TestContext.Current.CancellationToken);

            Assert.Equal(0, counts.Total);
            Assert.Equal(0, counts.Completed);
        }
    }
}
