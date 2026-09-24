using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;


namespace Musify.Application.PlayLists.Responses
{
    public record PlayListApplicationResponse(
        Guid Id,
        string Name,
        string? Description,
        string? SmallImageKeyName,
        string? MediumImageKeyName,
        string? LargeImageKeyName,
        PlayListVisibility Visibility,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IReadOnlyList<Guid> CoverTrackIds,
        long OwnerUserId,
        int TrackCount,
        double DurationSeconds)
    {
        public const int CoverTrackCount = PlayListProjections.CoverTrackCount;

        public static PlayListApplicationResponse FromEntity(
            PlayList playList,
            IReadOnlyList<Guid>? coverTrackIds = null,
            int trackCount = 0,
            double durationSeconds = 0)
        {
            return new PlayListApplicationResponse(
                playList.Id,
                playList.Name,
                playList.Description,
                playList.Pictures?.SmallName,
                playList.Pictures?.MediumName,
                playList.Pictures?.LargeName,
                playList.Visibility,
                playList.CreatedAt,
                playList.UpdatedAt,
                coverTrackIds ?? [],
                playList.OwnerUserId,
                trackCount,
                durationSeconds);
        }
    }
}
