using Musify.Application.Events;

namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record PublishTrackProcessingEventsArguments(
        Guid TrackId,
        string Bucket,
        string FinalPictureKey,
        string FinalAudioKey,
        string AudioDestinationFolderKey,
        ImageSize Small,
        ImageSize Medium,
        ImageSize Large);
}
