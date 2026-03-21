using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Application;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Queries
{
    public record GetPlayListsQuery(int PageNumber = 1, int PageSize = 10)
        : IRequest<GetPlayListsQuery, Task<Result<PaginatedResponse<PlayListResponse>> >>;
}