using Musify.Application.Shared;

namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record PublishTrackProcessingEventsArguments(
        Guid TrackId,
        string Bucket,
        string FinalPictureKey,
        string FinalAudioKey,
        string AudioDestinationFolderKey,
        ImageSizes Sizes);
}
