using Ardalis.Result;
using Mediator;
using Musify.Application.Abstractions.Application;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Queries
{
    public record GetPlayListsQuery(int PageNumber = 1, int PageSize = 10)
        : IQuery<Result<PaginatedResponse<PlayListApplicationResponse>> >;
}