using System.Text.Json.Serialization;
using Musify.Application.Serialization;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks.Responses
{
    public record LocalTrackApplicationResponse(
        Guid Id,
        string Title,
        string? Artist,
        ProcessingStatus AudioStatus,
        double Duration,
        int ListensCount,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        [property: JsonConverter(typeof(LongAsStringConverter))] long OwnerUserId)
        : TrackApplicationResponse(Id, Title, Artist, AudioStatus, Duration, ListensCount, CreatedAt, UpdatedAt)
    {
        public static LocalTrackApplicationResponse FromEntity(LocalTrack track, int listensCount) =>
            new(
                track.Id,
                track.Title,
                FormatArtist(track),
                track.Audio.TranscodeStatus,
                track.DurationSeconds,
                listensCount,
                track.CreatedAt,
                track.UpdatedAt,
                track.OwnerUserId);
    }
}
