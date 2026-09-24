using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Albums
{
    public record GetAlbumTrackIdsQuery(long UserId, Guid AlbumId)
        : IQuery<ErrorOr<AlbumTrackIdsResponse>>;

    public class GetAlbumTrackIdsQueryHandler(IDatabase database)
        : IQueryHandler<GetAlbumTrackIdsQuery, ErrorOr<AlbumTrackIdsResponse>>
    {
        public async ValueTask<ErrorOr<AlbumTrackIdsResponse>> Handle(GetAlbumTrackIdsQuery request, CancellationToken cancellationToken)
        {
            var found = await database.Albums.FindOwnedAsync(request.AlbumId, request.UserId, cancellationToken);
            if (found.IsError)
                return found.Errors;

            var trackIds = await database.AlbumHasTracks
                .AsNoTracking()
                .Where(albumTrack => albumTrack.AlbumId == request.AlbumId
                    && albumTrack.Track.LifeCycleStatus == LifeCycleStatus.Active)
                .OrderBy(albumTrack => albumTrack.TrackNumber)
                .ThenBy(albumTrack => albumTrack.Id)
                .Select(albumTrack => albumTrack.TrackId)
                .ToListAsync(cancellationToken);

            return new AlbumTrackIdsResponse(trackIds);
        }
    }
}
