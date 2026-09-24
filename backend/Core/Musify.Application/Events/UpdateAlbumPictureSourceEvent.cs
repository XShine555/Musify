using Musify.Application.Shared;

namespace Musify.Application.Events;

public record UpdateAlbumPictureSourceEvent(
    Guid AlbumId,
    Guid PictureIntentId,
    string Bucket,
    string PictureSourceKey,
    string PictureDestinationKey,
    ImageSizes Sizes);
