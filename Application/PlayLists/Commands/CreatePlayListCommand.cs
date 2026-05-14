using Ardalis.Result;
using Mediator;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Commands
{
    public record CreatePlayListCommand(
        Guid UserId,
        string Name,
        string Description,
        string? OriginalPictureName)
        : ICommand<Result<PlayListApplicationResponse>>;
}