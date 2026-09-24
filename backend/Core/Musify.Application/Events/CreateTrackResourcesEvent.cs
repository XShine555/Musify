using Musify.Application.Shared;

namespace Musify.Application.Events;

public record CreateTrackResourcesEvent(
    Guid TrackId,
    Guid PictureIntentId,
    Guid AudioIntentId,
    string Bucket,
    string PictureSourceKey,
    string PictureDestinationKey,
    string AudioSourceKey,
    string AudioDestinationKey,
    string AudioProcessedFolderKey,
    ImageSize Small,
    ImageSize Medium,
    ImageSize Large);
