using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks.Responses
{
    public record ExternalTrackApplicationResponse(
        Guid Id,
        string Title,
        string? Artist,
        ProcessingStatus AudioStatus,
        double Duration,
        int ListensCount,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string ExternalId)
        : TrackApplicationResponse(Id, Title, Artist, AudioStatus, Duration, ListensCount, CreatedAt, UpdatedAt)
    {
        public static ExternalTrackApplicationResponse FromEntity(ExternalTrack track, int listensCount) =>
            new(
                track.Id,
                track.Title,
                FormatArtist(track),
                track.Audio.TranscodeStatus,
                track.DurationSeconds,
                listensCount,
                track.CreatedAt,
                track.UpdatedAt,
                track.ExternalId);
    }
}
