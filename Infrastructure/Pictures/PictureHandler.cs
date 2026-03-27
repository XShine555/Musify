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
        public async Task<Result<Stream>> ResizePictureAsWebpAsync(Stream pictureStream, int width, int height, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogDebug("Resizing picture to {Width}x{Height}", width, height);
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

                logger.LogDebug("Picture resized successfully. OutputLength: {Length}", memoryStream.Length);
                return Result<Stream>.Success(memoryStream);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occurred while resizing the picture to {Width}x{Height}.", width, height);
                return Result.Error("An error occurred while resizing the picture.");
            }
        }
    }
}