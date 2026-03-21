using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Application;
using Musify.Application.Tracks.Contracts;

namespace Musify.Application.Tracks.Commands.CreateTrack
{
    public record CreateTrackCommand(
        Guid UserId,
        string Title,
        IFileData Picture,
        IFileData Audio)
        : IRequest<CreateTrackCommand, Task<Result<TrackResponse> >>;
}