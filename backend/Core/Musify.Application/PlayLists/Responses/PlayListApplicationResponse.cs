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
        PlaylistVisibility Visibility,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IReadOnlyList<Guid> CoverTrackIds)
    {
        public const int CoverTrackCount = 4;

        public static PlayListApplicationResponse FromEntity(
            PlayList playList,
            IReadOnlyList<Guid>? coverTrackIds = null)
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
                coverTrackIds ?? []);
        }
    }
}
