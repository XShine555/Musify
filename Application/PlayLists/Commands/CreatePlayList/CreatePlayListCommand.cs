using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Application;
using Musify.Application.PlayLists.Contracts;

namespace Musify.Application.PlayLists.Commands.CreatePlayList
{
    public record CreatePlayListCommand(
        Guid UserId,
        string Name,
        string Description,
        IFileData? Picture)
        : IRequest<CreatePlayListCommand, Task<Result<PlayListResponse>> >;
}