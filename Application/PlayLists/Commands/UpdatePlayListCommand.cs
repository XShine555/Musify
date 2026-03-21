using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Application;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Commands
{
    public record UpdatePlayListCommand(
        Guid UserId,
        Guid PlayListId,
        string? NewName,
        string? NewDescription,
        IFileData? NewPicture)
        : IRequest<UpdatePlayListCommand, Task<Result<PlayListResponse>> >;
}