using Ardalis.Result;
using Mediator;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Commands
{
    public record UpdatePlayListCommand(
        Guid UserId,
        Guid PlayListId,
        string? NewName,
        string? NewDescription,
        Guid? NewPictureIntentId)
        : ICommand<Result<PlayListApplicationResponse>>;
}
