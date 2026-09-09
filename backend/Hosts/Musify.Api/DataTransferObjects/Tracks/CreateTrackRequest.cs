namespace Musify.Api.DataTransferObjects.Tracks;

public record CreateTrackRequest(
    string Title,
    Guid PictureIntentId,
    Guid AudioIntentId);
