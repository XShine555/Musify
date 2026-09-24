using Musify.Application.Shared;

namespace Musify.Application.Events;

public record UpdatePlayListPictureEvent(
    Guid PlayListId,
    string Bucket,
    string SourceKey,
    ImageSizes Sizes);
