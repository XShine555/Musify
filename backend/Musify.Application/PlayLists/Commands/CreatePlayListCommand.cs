using ErrorOr;
using Mediator;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Commands
{
    public record CreatePlayListCommand(
        long UserId,
        string Name,
        string Description,
        Guid? PictureIntentId)
        : ICommand<ErrorOr<PlayListApplicationResponse>>;
}
