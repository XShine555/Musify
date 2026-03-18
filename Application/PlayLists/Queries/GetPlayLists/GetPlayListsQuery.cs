using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.PlayLists.Contracts;

namespace Musify.Application.PlayLists.Queries.GetPlayLists
{
    public record GetPlayListsQuery(int PageNumber = 1, int PageSize = 10)
        : IRequest<GetPlayListsQuery, Result<PaginatedPlayListResponse>>;
}