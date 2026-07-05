using Musify.Domain.Entities;

namespace Musify.Application.Tracks.Responses
{
    public record TrackApplicationResponse(
        Guid Id,
        string Title,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public static TrackApplicationResponse FromEntity(Track track)
        {
            return new TrackApplicationResponse(
                track.Id,
                track.Title,
                track.CreatedAt,
                track.UpdatedAt);
        }
    }
}