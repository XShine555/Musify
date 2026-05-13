using Musify.Domain.Entities;

namespace Musify.Application.Tracks.Responses
{
    public record TrackApplicationResponse(
        Guid Id,
        string Title,
        DateTime CreatedDate,
        DateTime UpdatedDate)
    {
        public static TrackApplicationResponse FromEntity(Track track)
        {
            return new TrackApplicationResponse(
                track.Id,
                track.Title,
                track.CreatedDate,
                track.UpdatedDate);
        }
    }
}