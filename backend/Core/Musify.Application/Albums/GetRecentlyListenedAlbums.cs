using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;

namespace Musify.Application.Albums;

public record GetRecentlyListenedAlbumsQuery(long UserId, int Limit)
    : IQuery<ErrorOr<IReadOnlyList<AlbumApplicationResponse>>>;

public class GetRecentlyListenedAlbumsQueryHandler(IDatabase database)
    : IQueryHandler<GetRecentlyListenedAlbumsQuery, ErrorOr<IReadOnlyList<AlbumApplicationResponse>>>
{
    public async ValueTask<ErrorOr<IReadOnlyList<AlbumApplicationResponse>>> Handle(GetRecentlyListenedAlbumsQuery request, CancellationToken cancellationToken)
    {
        var listenedTrackIds = database.ListeningHistories
            .AsNoTracking()
            .Where(history => history.UserId == request.UserId && history.IsCounted);

        var ranked = await database.AlbumHasTracks
            .AsNoTracking()
            .Join(
                listenedTrackIds,
                albumTrack => albumTrack.TrackId,
                history => history.TrackId,
                (albumTrack, history) => new { albumTrack.AlbumId, history.ListenedAt })
            .GroupBy(entry => entry.AlbumId)
            .Select(group => new { AlbumId = group.Key, LastListenedAt = group.Max(entry => entry.ListenedAt) })
            .OrderByDescending(entry => entry.LastListenedAt)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        if (ranked.Count == 0)
            return new List<AlbumApplicationResponse>();

        var albumIds = ranked.Select(entry => entry.AlbumId).ToList();

        var albums = await database.Albums
            .AsNoTracking()
            .Where(album => albumIds.Contains(album.Id))
            .Select(album => new
            {
                Album = album,
                TrackCount = album.AlbumTracks.Count,
                CoverTrackIds = album.AlbumTracks
                    .OrderBy(albumTrack => albumTrack.TrackNumber)
                    .Take(AlbumApplicationResponse.CoverTrackCount)
                    .Select(albumTrack => albumTrack.TrackId)
                    .ToList()
            })
            .ToDictionaryAsync(entry => entry.Album.Id, cancellationToken);

        return ranked
            .Where(entry => albums.ContainsKey(entry.AlbumId))
            .Select(entry => albums[entry.AlbumId])
            .Select(entry => AlbumApplicationResponse.FromEntity(entry.Album, entry.TrackCount, entry.CoverTrackIds))
            .ToList();
    }
}
