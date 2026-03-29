using Ardalis.Result;
using Mediator;

namespace Musify.Application.PlayLists.Commands
{
    public record DeletePlayListCommand(Guid UserId, Guid PlayListId)
        : ICommand<Result>;
}