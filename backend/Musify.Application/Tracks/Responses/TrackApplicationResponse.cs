using Musify.Domain.Entities;

namespace Musify.Application.Tracks.Responses
{
    public record TrackApplicationResponse(
        Guid Id,
        string Title,
        int Duration,
        int ListensCount,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public static TrackApplicationResponse FromEntity(Track track, int listensCount) =>
            new(
                track.Id,
                track.Title,
                track.Duration,
                listensCount,
                track.CreatedAt,
                track.UpdatedAt);
    }
}