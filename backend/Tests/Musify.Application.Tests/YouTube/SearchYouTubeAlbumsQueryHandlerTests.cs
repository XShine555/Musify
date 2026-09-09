using ErrorOr;
using Musify.Application.Contracts;
using Musify.Application.YouTube;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.YouTube;

public sealed class SearchYouTubeAlbumsQueryHandlerTests
{
    private readonly IYouTubeMusicService youTubeMusicService = Substitute.For<IYouTubeMusicService>();

    private SearchYouTubeAlbumsQueryHandler CreateHandler() => new(youTubeMusicService);

    [Fact]
    public async Task Handle_EmptyQuery_ReturnsValidationErrorWithoutCallingTheService()
    {
        var result = await CreateHandler().Handle(new SearchYouTubeAlbumsQuery(string.Empty, string.Empty), CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        await youTubeMusicService.DidNotReceiveWithAnyArgs()
            .SearchAlbumsAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_NonEmptyQuery_DelegatesToTheService()
    {
        var expected = new YouTubeAlbumSearchResult([], "next-token");
        youTubeMusicService.SearchAlbumsAsync("query", "token", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await CreateHandler().Handle(new SearchYouTubeAlbumsQuery("query", "token"), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Same(expected, result.Value);
    }
}
