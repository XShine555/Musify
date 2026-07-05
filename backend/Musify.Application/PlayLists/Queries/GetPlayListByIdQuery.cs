using Ardalis.Result;
using Mediator;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Queries
{
    public record GetPlayListByIdQuery(Guid Id)
        : IQuery<Result<PlayListApplicationResponse>>;
}