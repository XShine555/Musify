using Ardalis.Result;
using DispatchR.Abstractions.Send;

namespace Musify.Application.Tracks.Commands
{
    public record DeleteTrackCommand(
        Guid UserId,
        Guid TrackId)
        : IRequest<DeleteTrackCommand, Task<Result>>;
}