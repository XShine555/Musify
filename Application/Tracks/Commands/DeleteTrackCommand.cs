using Ardalis.Result;
using Mediator;

namespace Musify.Application.Tracks.Commands
{
    public record DeleteTrackCommand(
        Guid UserId,
        Guid TrackId)
        : ICommand<Result>;
}