using Ardalis.Result;
using Mediator;
using Musify.Application.Pagination;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Queries
{
    public record GetTracksQuery(int PageNumber = 1, int PageSize = 10)
        : IQuery<Result<PaginatedResponse<TrackApplicationResponse> >>;
}