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
        public static TrackApplicationResponse FromEntity(Track track)
        {
            var count = track.ListeningHistories.Count(x => x.TrackId == track.Id);

            return new TrackApplicationResponse(
                track.Id,
                track.Title,
                track.Duration,
                count,
                track.CreatedAt,
                track.UpdatedAt);
        }
    }
}