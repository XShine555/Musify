using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Musify.Infrastructure.Pictures
{
    public class PictureHandler(ILogger<PictureHandler> logger)
        : IPictureHandler
    {
        public async Task<Result<Stream>> ResizePictureAsync(Stream pictureStream, int width, int height, CancellationToken cancellationToken)
        {
            try
            {
                if (pictureStream.CanSeek && pictureStream.Position > 0)
                    pictureStream.Position = 0;

                using var picture = await Image.LoadAsync(pictureStream, cancellationToken);

                picture.Mutate(options => options.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Max
                } ));

                var memoryStream = new MemoryStream();

                await picture.SaveAsWebpAsync(memoryStream, cancellationToken);

                memoryStream.Position = 0;

                return Result<Stream>.Success(memoryStream);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occurred while resizing the picture.");
                return Result.Error("An error occurred while resizing the picture.");
            }
        }
    }
}