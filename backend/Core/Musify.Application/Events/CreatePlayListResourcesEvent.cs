using Musify.Application.Shared;

namespace Musify.Application.Events;

public record CreatePlayListResourcesEvent(
    Guid PlayListId,
    Guid PictureIntentId,
    string Bucket,
    string PictureSourceKey,
    string PictureDestinationKey,
    ImageSize Small,
    ImageSize Medium,
    ImageSize Large);
