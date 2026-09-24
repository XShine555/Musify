using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists.Responses
{
    public static class PlayListProjections
    {
        public const int CoverTrackCount = 4;

        public static IQueryable<PlayList> Active(this IQueryable<PlayList> query) =>
            query.Where(playList => playList.LifeCycleStatus == LifeCycleStatus.Active);

        public static IQueryable<PlayList> VisibleTo(this IQueryable<PlayList> query, long? viewerId) =>
            query.Where(playList => playList.Visibility == PlayListVisibility.Public || playList.OwnerUserId == viewerId);

        public static IQueryable<PlayListApplicationResponse> SelectResponse(this IQueryable<PlayList> query) =>
            query.Select(playList => new PlayListApplicationResponse(
                playList.Id,
                playList.Name,
                playList.Description,
                playList.Pictures!.SmallName,
                playList.Pictures!.MediumName,
                playList.Pictures!.LargeName,
                playList.Visibility,
                playList.CreatedAt,
                playList.UpdatedAt,
                playList.PlayListTracks
                    .OrderBy(playListTrack => playListTrack.Position)
                    .Take(CoverTrackCount)
                    .Select(playListTrack => playListTrack.TrackId)
                    .ToList(),
                playList.OwnerUserId,
                playList.PlayListTracks.Count(playListTrack => playListTrack.Track.LifeCycleStatus == LifeCycleStatus.Active),
                playList.PlayListTracks
                    .Where(playListTrack => playListTrack.Track.LifeCycleStatus == LifeCycleStatus.Active)
                    .Sum(playListTrack => playListTrack.Track.DurationSeconds)));
    }
}
