using Ardalis.Result;
using Mediator;

namespace Musify.Application.PlayLists.Commands
{
    public record AddTrackToPlayListCommand(long UserId, Guid PlayListId, Guid TrackId)
        : ICommand<Result>;
}
