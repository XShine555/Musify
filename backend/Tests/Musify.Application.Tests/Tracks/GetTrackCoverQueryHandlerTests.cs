using ErrorOr;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Xunit;

namespace Musify.Application.Tests.Tracks;

public sealed class GetTrackCoverQueryHandlerTests : HandlerTestBase
{
    private GetTrackCoverQueryHandler CreateHandler() =>
        new(Database, TestConfigurations.Storage("covers-bucket"), TestConfigurations.Track());

    [Theory]
    [InlineData("small", "small.webp")]
    [InlineData("medium", "medium.webp")]
    [InlineData("large", "large.webp")]
    [InlineData("unknown-size-falls-back-to-medium", "medium.webp")]
    public async Task Handle_ProcessedTrack_ReturnsRequestedSize(string size, string expectedFileName)
    {
        var owner = TestEntities.User();
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, track);

        var result = await CreateHandler().Handle(new GetTrackCoverQuery(track.Id, size), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("covers-bucket", result.Value.Bucket);
        Assert.EndsWith(expectedFileName, result.Value.Key);
        Assert.Equal("image/webp", result.Value.ContentType);
    }

    [Fact]
    public async Task Handle_TrackMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new GetTrackCoverQuery(Guid.NewGuid(), "small"), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_PictureNotProcessedYet_ReturnsNotFound()
    {
        var owner = TestEntities.User();
        var track = TestEntities.Track(owner, pictures: TestEntities.PendingPictures());
        await SeedAsync(owner, track);

        var result = await CreateHandler().Handle(new GetTrackCoverQuery(track.Id, "small"), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
