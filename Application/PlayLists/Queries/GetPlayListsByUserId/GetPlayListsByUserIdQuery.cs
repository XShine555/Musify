using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Application;
using Musify.Application.PlayLists.Contracts;

namespace Musify.Application.PlayLists.Queries.GetPlayListsByUserId
{
    public record GetPlayListsByUserIdQuery(
        Guid UserId,
        string Name = "",
        int PageNumber = 1,
        int PageSize = 10)
        : IRequest<GetPlayListsByUserIdQuery, Task<Result<PaginatedResponse<PlayListResponse>> >>;
}