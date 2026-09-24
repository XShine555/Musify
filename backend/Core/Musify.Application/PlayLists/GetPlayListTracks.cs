using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists
{
    public record GetPlayListTracksQuery(
        Guid PlayListId,
        int PageNumber,
        int PageSize,
        long? ViewerId = null)
        : IQuery<ErrorOr<PaginatedResponse<TrackApplicationResponse>>>;

    public class GetPlayListTracksQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListTracksQuery, ErrorOr<PaginatedResponse<TrackApplicationResponse>>>
    {
        public async ValueTask<ErrorOr<PaginatedResponse<TrackApplicationResponse>>> Handle(GetPlayListTracksQuery request, CancellationToken cancellationToken)
        {
            var playListVisible = await database.PlayLists
                .AsNoTracking()
                .Active()
                .VisibleTo(request.ViewerId)
                .AnyAsync(playList => playList.Id == request.PlayListId, cancellationToken);
            if (!playListVisible)
                return AppErrors.NotFound("PlayList", request.PlayListId);

            return await database.PlayListHasTracks
                .AsNoTracking()
                .Where(playListTrack => playListTrack.PlayListId == request.PlayListId && playListTrack.Track.LifeCycleStatus == LifeCycleStatus.Active)
                .OrderBy(playListTrack => playListTrack.Position)
                .ThenBy(playListTrack => playListTrack.Id)
                .Select(playListTrack => playListTrack.Track)
                .SelectResponse()
                .ToPaginatedAsync(new PageRequest(request.PageNumber, request.PageSize), cancellationToken);
        }
    }
}
