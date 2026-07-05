using Ardalis.Result;
using Mediator;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Commands
{
    public record CreateTrackCommand(
        long UserId,
        string Title,
        Guid PictureIntentId,
        Guid AudioIntentId)
        : ICommand<Result<TrackApplicationResponse>>;
}
