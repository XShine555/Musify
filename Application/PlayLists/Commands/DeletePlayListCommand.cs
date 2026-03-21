using Ardalis.Result;
using DispatchR.Abstractions.Send;

namespace Musify.Application.PlayLists.Commands
{
    public record DeletePlayListCommand(Guid UserId, Guid PlayListId)
        : IRequest<DeletePlayListCommand, Task<Result> >;
}