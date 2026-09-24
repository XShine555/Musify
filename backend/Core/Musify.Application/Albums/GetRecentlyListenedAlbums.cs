using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;

namespace Musify.Application.Albums
{
    public record GetRecentlyListenedAlbumsQuery(long UserId, int Limit)
        : IQuery<IReadOnlyList<AlbumApplicationResponse>>;

    public class GetRecentlyListenedAlbumsQueryHandler(IDatabase database)
        : IQueryHandler<GetRecentlyListenedAlbumsQuery, IReadOnlyList<AlbumApplicationResponse>>
    {
        public async ValueTask<IReadOnlyList<AlbumApplicationResponse>> Handle(GetRecentlyListenedAlbumsQuery request, CancellationToken cancellationToken)
        {
            var listens = database.ListeningHistories
                .AsNoTracking()
                .Where(history => history.UserId == request.UserId && history.IsCounted);

            var ranked = await database.AlbumHasTracks
                .AsNoTracking()
                .Where(albumTrack => albumTrack.Album.LifeCycleStatus == Domain.ValueObjects.LifeCycleStatus.Active)
                .Join(
                    listens,
                    albumTrack => albumTrack.TrackId,
                    history => history.TrackId,
                    (albumTrack, history) => new { albumTrack.AlbumId, history.ListenedAt })
                .GroupBy(entry => entry.AlbumId)
                .Select(group => new { AlbumId = group.Key, LastListenedAt = group.Max(entry => entry.ListenedAt) })
                .OrderByDescending(entry => entry.LastListenedAt)
                .ThenBy(entry => entry.AlbumId)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);

            if (ranked.Count == 0)
                return [];

            var albumIds = ranked.Select(entry => entry.AlbumId).ToList();

            var albumsById = await database.Albums
                .AsNoTracking()
                .Where(album => albumIds.Contains(album.Id))
                .SelectResponse()
                .ToDictionaryAsync(album => album.Id, cancellationToken);

            return ranked
                .Where(entry => albumsById.ContainsKey(entry.AlbumId))
                .Select(entry => albumsById[entry.AlbumId])
                .ToList();
        }
    }
}
