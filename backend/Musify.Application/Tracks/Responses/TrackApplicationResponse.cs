using System.Linq;
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
                FormatArtist(track),
                track.Source,
                track.ExternalId,
                track.Audio.TranscodeStatus,
                track.DurationSeconds,
                listensCount,
                track.OwnerUserId,
                track.CreatedAt,
                track.UpdatedAt);

        private static string? FormatArtist(Track track)
        {
            if (track.TrackArtists.Count == 0)
                return null;

            return string.Join(", ", track.TrackArtists
                .OrderBy(trackArtist => trackArtist.Position)
                .Select(trackArtist => trackArtist.Artist.Name));
        }
    }
}
