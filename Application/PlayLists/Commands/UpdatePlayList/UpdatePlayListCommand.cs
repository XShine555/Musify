using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Application;
using Musify.Application.PlayLists.Contracts;

namespace Musify.Application.PlayLists.Commands.UpdatePlayList
{
    public record UpdatePlayListCommand(
        Guid UserId,
        Guid PlayListId,
        string? NewName,
        string? NewDescription,
        IFileData? NewPicture)
        : IRequest<UpdatePlayListCommand, Task<Result<PlayListResponse>> >;
}