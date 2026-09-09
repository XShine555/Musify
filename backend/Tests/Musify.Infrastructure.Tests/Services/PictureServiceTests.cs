using Microsoft.Extensions.Logging.Abstractions;
using Musify.Infrastructure.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Musify.Infrastructure.Tests.Services;

public sealed class PictureServiceTests
{
    private static PictureService CreateService() => new(NullLogger<PictureService>.Instance);

    private static MemoryStream CreateSourceImage(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(255, 0, 0));
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task ResizePictureAsWebpAsync_LargerThanTarget_ShrinksToFitWithinTheRequestedBox()
    {
        using var source = CreateSourceImage(800, 600);

        await using var resized = await CreateService().ResizePictureAsWebpAsync(source, 200, 200, TestContext.Current.CancellationToken);

        using var result = await Image.LoadAsync(resized, TestContext.Current.CancellationToken);
        Assert.InRange(result.Width, 1, 200);
        Assert.InRange(result.Height, 1, 200);
        Assert.True(result.Width == 200 || result.Height == 200, "expected the longer side to land exactly on the target box");
    }

    [Fact]
    public async Task ResizePictureAsWebpAsync_ReturnsAValidWebpStream()
    {
        using var source = CreateSourceImage(100, 100);

        await using var resized = await CreateService().ResizePictureAsWebpAsync(source, 50, 50, TestContext.Current.CancellationToken);

        var format = await Image.DetectFormatAsync(resized, TestContext.Current.CancellationToken);
        Assert.Equal("WEBP", format?.Name, ignoreCase: true);
    }

    [Fact]
    public async Task ResizePictureAsWebpAsync_PreservesAspectRatio()
    {
        using var source = CreateSourceImage(400, 200);

        await using var resized = await CreateService().ResizePictureAsWebpAsync(source, 100, 100, TestContext.Current.CancellationToken);

        using var result = await Image.LoadAsync(resized, TestContext.Current.CancellationToken);
        Assert.Equal(100, result.Width);
        Assert.Equal(50, result.Height);
    }
}
