using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Queries
{
    public record GetPlayListByIdQuery(Guid Id)
        : IRequest<GetPlayListByIdQuery, Task<Result<PlayListResponse>>>;
}