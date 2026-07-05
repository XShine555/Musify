using ErrorOr;
using Mediator;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Commands
{
    public record UpdatePlayListCommand(
        long UserId,
        Guid PlayListId,
        string? NewName,
        string? NewDescription,
        Guid? NewPictureIntentId)
        : ICommand<ErrorOr<PlayListApplicationResponse>>;
}
