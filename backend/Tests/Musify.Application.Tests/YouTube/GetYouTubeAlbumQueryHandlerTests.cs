using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using Musify.Application.YouTube;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.YouTube;

public sealed class GetYouTubeAlbumQueryHandlerTests : HandlerTestBase
{
    private readonly IYouTubeMusicService youTubeMusicService = Substitute.For<IYouTubeMusicService>();

    private GetYouTubeAlbumQueryHandler CreateHandler()
    {
        var provisioner = new YouTubeTrackProvisioner(
            Database, youTubeMusicService, TestConfigurations.Track(), NoOpLogger<YouTubeTrackProvisioner>());

        return new GetYouTubeAlbumQueryHandler(youTubeMusicService, provisioner, NoOpLogger<GetYouTubeAlbumQueryHandler>());
    }

    private static YouTubeAlbumDetail BuildDetail() => new(
        new YouTubeAlbumResult("album-1", "Album Title", "Album Artist", "https://img/thumb.jpg", 2024, false, false, []),
        Description: "An album",
        TotalDurationSeconds: 600,
        Tracks: [new YouTubeAlbumTrack("track-1", "Track One", 200, 1, false)]);

    [Fact]
    public async Task Handle_EmptyAlbumId_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new GetYouTubeAlbumQuery(string.Empty), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_ServiceSucceeds_MaterializesAlbumAndReturnsDetail()
    {
        var detail = BuildDetail();
        youTubeMusicService.GetAlbumAsync("album-1", Arg.Any<CancellationToken>()).Returns(detail);

        var result = await CreateHandler().Handle(new GetYouTubeAlbumQuery("album-1"), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Same(detail, result.Value);
        Assert.NotEmpty(await Database.ExternalAlbums.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ServiceFails_ReturnsError()
    {
        youTubeMusicService.GetAlbumAsync("missing", Arg.Any<CancellationToken>())
            .Returns(Error.NotFound(description: "Album not found"));

        var result = await CreateHandler().Handle(new GetYouTubeAlbumQuery("missing"), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
    }
}
