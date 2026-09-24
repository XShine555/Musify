using Musify.Application.Shared;

namespace Musify.Application.Events;

public record CreateAlbumResourcesEvent(
    Guid AlbumId,
    Guid PictureIntentId,
    string Bucket,
    string PictureSourceKey,
    string PictureDestinationKey,
    ImageSizes Sizes);
