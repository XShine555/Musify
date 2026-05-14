using Ardalis.Result;
using Mediator;
using Musify.Application.Pagination;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Queries
{
    public record GetPlayListsByUserIdQuery(
        Guid UserId,
    string? Name = null,
        int PageNumber = 1,
        int PageSize = 10)
        : IQuery<Result<PaginatedResponse<PlayListApplicationResponse>> >;
}