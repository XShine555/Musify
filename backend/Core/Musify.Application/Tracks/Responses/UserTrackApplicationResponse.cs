using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks.Responses
{
    public record TrackAlbumReference(Guid Id, string Title);

    public record UserTrackApplicationResponse(
        Guid Id,
        string Title,
        string? Artist,
        ProcessingStatus AudioStatus,
        double Duration,
        int ListensCount,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        long OwnerUserId,
        IReadOnlyCollection<Genre> Tags,
        bool IsExplicit,
        bool IsPublished,
        IReadOnlyList<TrackAlbumReference> Albums);
}
