namespace Musify.Application.Events;

public record PlayListProcessingFailed(
    Guid PlayListId,
    string? Bucket,
    string? PictureKey);
