using ErrorOr;
using Musify.Infrastructure.Services;
using Xunit;

namespace Musify.Infrastructure.Tests.Services;

public sealed class DisabledYouTubeMusicServiceTests
{
    private readonly DisabledYouTubeMusicService service = new();

    [Fact]
    public async Task SearchSongsAsync_ReturnsForbidden()
    {
        var result = await service.SearchSongsAsync("query", string.Empty, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
    }

    [Fact]
    public async Task SearchAlbumsAsync_ReturnsForbidden()
    {
        var result = await service.SearchAlbumsAsync("query", string.Empty, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
    }

    [Fact]
    public async Task GetAlbumAsync_ReturnsForbidden()
    {
        var result = await service.GetAlbumAsync("album-1", TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
    }

    [Fact]
    public async Task GetAudioStreamAsync_ReturnsForbidden()
    {
        var result = await service.GetAudioStreamAsync("video-1", TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
    }

    [Fact]
    public async Task GetSongAsync_ReturnsForbidden()
    {
        var result = await service.GetSongAsync("video-1", TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
    }

    [Fact]
    public void ResolveArtworkUrl_ReturnsTheUrlUnchanged()
    {
        var result = service.ResolveArtworkUrl("https://img.example/thumb.jpg");

        Assert.Equal("https://img.example/thumb.jpg", result);
    }
}
