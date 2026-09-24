using System.Text.Json.Serialization;
using Musify.Application.Serialization;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks.Responses;

public record TrackApplicationResponse(
    Guid Id,
    string Title,
    string? Artist,
    ProcessingStatus AudioStatus,
    double Duration,
    int ListensCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    [property: JsonConverter(typeof(LongAsStringConverter))] long OwnerUserId,
    IReadOnlyCollection<Genre> Tags,
    bool IsExplicit)
{
    public static TrackApplicationResponse FromEntity(Track track, int listensCount) =>
        new(
            track.Id,
            track.Title,
            track.Owner.Name,
            track.Audio.TranscodeStatus,
            track.DurationSeconds,
            listensCount,
            track.CreatedAt,
            track.UpdatedAt,
            track.OwnerUserId,
            track.Tags.Select(tag => tag.Tag).ToList(),
            track.IsExplicit);
}
