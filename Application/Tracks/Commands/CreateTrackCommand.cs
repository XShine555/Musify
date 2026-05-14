using Ardalis.Result;
using Mediator;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Commands
{
    public record CreateTrackCommand(
        Guid UserId,
        string Title,
        string OriginalPictureName,
        string OriginalAudioName)
        : ICommand<Result<TrackApplicationResponse>>;
}