using Musify.Application.Shared;

namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record PublishAlbumPictureProcessingEventArguments(
        Guid AlbumId,
        string Bucket,
        string FinalPictureKey,
        ImageSizes Sizes);
}
