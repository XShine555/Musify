using ErrorOr;
using Mediator;

namespace Musify.Application.PlayLists.Commands
{
    public record DeletePlayListCommand(long UserId, Guid PlayListId)
        : ICommand<ErrorOr<Success>>;
}