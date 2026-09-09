using Musify.Application.Shared;

namespace Musify.Application.Events
{
    public record DownloadYouTubeTrackEvent(
        Guid TrackId,
        string VideoId,
        string ThumbnailUrl,
        string Bucket,
        string AudioProcessedFolderKey,
        ImageSize Small,
        ImageSize Medium,
        ImageSize Large);
}
