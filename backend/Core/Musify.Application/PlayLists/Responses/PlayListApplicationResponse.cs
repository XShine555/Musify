using System.Text.Json.Serialization;
using Musify.Application.Serialization;
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
        [property: JsonConverter(typeof(LongAsStringConverter))] long OwnerUserId,
        PlaylistVisibility Visibility,
        int TrackCount,
        double DurationSeconds,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IReadOnlyList<Guid> CoverTrackIds)
    {
        public const int CoverTrackCount = 4;

        public static PlayListApplicationResponse FromEntity(
            PlayList playList,
            int trackCount = 0,
            double durationSeconds = 0,
            IReadOnlyList<Guid>? coverTrackIds = null)
        {
            return new PlayListApplicationResponse(
                playList.Id,
                playList.Name,
                playList.Description,
                playList.Pictures?.SmallName,
                playList.Pictures?.MediumName,
                playList.Pictures?.LargeName,
                playList.UserId,
                playList.Visibility,
                trackCount,
                durationSeconds,
                playList.CreatedAt,
                playList.UpdatedAt,
                coverTrackIds ?? []);
        }
    }
}
