using Musify.Application.Shared;

namespace Musify.Application.Events
{
    public record UpdatePlayListPictureSourceEvent(
        Guid PlayListId,
        Guid PictureIntentId,
        string Bucket,
        string PictureSourceKey,
        string PictureDestinationKey,
        ImageSize Small,
        ImageSize Medium,
        ImageSize Large);
}
