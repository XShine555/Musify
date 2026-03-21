using Musify.Domain.Entities;

namespace Musify.Application.Tracks.Contracts
{
    public record TrackResponse(
        Guid Id,
        string Title,
        DateTime CreatedDate,
        DateTime UpdatedDate)
    {
        public static TrackResponse FromEntity(Track track)
        {
            return new TrackResponse(
                track.Id,
                track.Title,
                track.CreatedDate,
                track.UpdatedDate);
        }
    }
}