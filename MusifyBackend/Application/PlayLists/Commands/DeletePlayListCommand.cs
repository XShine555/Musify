using Ardalis.Result;
using Mediator;

namespace Musify.Application.PlayLists.Commands
{
    public record DeletePlayListCommand(long UserId, Guid PlayListId)
        : ICommand<Result>;
}