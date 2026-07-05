using ErrorOr;
using Mediator;
using Musify.Application.Pagination;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Queries
{
    public record GetPlayListsByUserIdQuery(
        long UserId,
        string? Name,
        int PageNumber = 1,
        int PageSize = 10)
        : IQuery<ErrorOr<PaginatedResponse<PlayListApplicationResponse>> >;
}