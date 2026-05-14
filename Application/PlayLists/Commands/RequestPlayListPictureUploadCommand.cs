using Ardalis.Result;
using Mediator;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Commands
{
    public record RequestPlayListPictureUploadCommand(
        Guid UserId,
        string FileType,
        string ContentType,
        long? ExpectedSizeBytes = null)
        : ICommand<Result<PlayListPictureUploadResponse>>;
}
