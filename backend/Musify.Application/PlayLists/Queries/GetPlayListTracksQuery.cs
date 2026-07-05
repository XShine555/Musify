using ErrorOr;
using Mediator;
using Musify.Application.Pagination;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.PlayLists.Queries
{
    public record GetPlayListTracksQuery(
        Guid PlayListId,
        int PageNumber = 1,
        int PageSize = 10)
        : IQuery<ErrorOr<PaginatedResponse<TrackApplicationResponse>>>;
}
