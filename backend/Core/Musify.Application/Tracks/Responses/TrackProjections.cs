using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks.Responses
{
    public static class TrackProjections
    {
        public static IQueryable<Track> Active(this IQueryable<Track> query) =>
            query.Where(track => track.LifeCycleStatus == LifeCycleStatus.Active);

        public static IQueryable<TrackApplicationResponse> SelectResponse(this IQueryable<Track> query) =>
            query.Select(track => new TrackApplicationResponse(
                track.Id,
                track.Title,
                track.Owner.Name,
                track.Audio.TranscodeStatus,
                track.DurationSeconds,
                track.ListeningHistories.Count(listen => listen.IsCounted),
                track.CreatedAt,
                track.UpdatedAt,
                track.OwnerUserId,
                track.Tags.Select(tag => tag.Tag).ToList(),
                track.IsExplicit));
    }
}
