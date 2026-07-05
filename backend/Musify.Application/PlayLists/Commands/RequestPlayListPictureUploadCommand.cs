using ErrorOr;
using Mediator;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Commands
{
    public record RequestPlayListPictureUploadCommand(
        long UserId,
        string FileType,
        string ContentType,
        long? ExpectedSizeBytes = null)
        : ICommand<ErrorOr<PlayListPictureUploadResponse>>;
}
