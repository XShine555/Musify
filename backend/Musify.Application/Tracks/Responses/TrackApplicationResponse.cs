using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks.Responses
{
    public record TrackApplicationResponse(
        Guid Id,
        string Title,
        string? Artist,
        TrackSource Source,
        string? ExternalId,
        ProcessingStatus AudioStatus,
        int Duration,
        int ListensCount,
        long? OwnerUserId,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public static TrackApplicationResponse FromEntity(Track track, int listensCount) =>
            new(
                track.Id,
                track.Title,
                track.Artist,
                track.Source,
                track.ExternalId,
                track.AudioTranscodeProcessingStatus,
                track.Duration,
                listensCount,
                track.OwnerUserId,
                track.CreatedAt,
                track.UpdatedAt);
    }
}
