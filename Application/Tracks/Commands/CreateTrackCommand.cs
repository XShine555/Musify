using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Application;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Commands
{
    public record CreateTrackCommand(
        Guid UserId,
        string Title,
        IFileData Picture,
        IFileData Audio)
        : IRequest<CreateTrackCommand, Task<Result<TrackResponse> >>;
}