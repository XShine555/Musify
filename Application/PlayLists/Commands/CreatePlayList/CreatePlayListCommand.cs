using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.PlayLists.Contracts;

namespace Musify.Application.PlayLists.Commands.CreatePlayList
{
    public record CreatePlayListCommand(
        Guid UserId,
        string Name,
        string Description,
        Stream PictureStream,
        string PictureFileType,
        string PictureContentType)
        : IRequest<CreatePlayListCommand, Result<PlayListResponse>>;
}