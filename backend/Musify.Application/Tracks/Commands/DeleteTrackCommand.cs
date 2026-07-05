using ErrorOr;
using Mediator;

namespace Musify.Application.Tracks.Commands
{
    public record DeleteTrackCommand(
        long UserId,
        Guid TrackId)
        : ICommand<ErrorOr<Success>>;
}