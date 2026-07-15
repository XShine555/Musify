namespace Musify.Api.DataTransferObjects.PlayLists;

public record AddYouTubeTrackRequest(
    string VideoId,
    string Title,
    string Artist,
    int DurationSeconds,
    string ThumbnailUrl);
