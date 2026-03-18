using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.PlayLists.Contracts;

namespace Musify.Application.PlayLists.Queries.GetPlayListById
{
    public record GetPlayListByIdQuery(Guid Id)
        : IRequest<GetPlayListByIdQuery, Result<PlayListResponse>>;
}