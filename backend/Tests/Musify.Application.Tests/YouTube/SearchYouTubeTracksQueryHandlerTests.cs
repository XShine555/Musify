using ErrorOr;
using Musify.Application.Contracts;
using Musify.Application.YouTube;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.YouTube;

public sealed class SearchYouTubeTracksQueryHandlerTests
{
    private readonly IYouTubeMusicService youTubeMusicService = Substitute.For<IYouTubeMusicService>();

    private SearchYouTubeTracksQueryHandler CreateHandler() => new(youTubeMusicService);

    [Fact]
    public async Task Handle_EmptyQuery_ReturnsValidationErrorWithoutCallingTheService()
    {
        var result = await CreateHandler().Handle(new SearchYouTubeTracksQuery(string.Empty, string.Empty), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        await youTubeMusicService.DidNotReceiveWithAnyArgs()
            .SearchSongsAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_NonEmptyQuery_DelegatesToTheService()
    {
        var expected = new YouTubeSearchResult([], "next-token");
        youTubeMusicService.SearchSongsAsync("query", "token", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await CreateHandler().Handle(new SearchYouTubeTracksQuery("query", "token"), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Same(expected, result.Value);
    }
}
