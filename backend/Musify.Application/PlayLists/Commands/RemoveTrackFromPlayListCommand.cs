using ErrorOr;
using Mediator;

namespace Musify.Application.PlayLists.Commands
{
    public record RemoveTrackFromPlayListCommand(long UserId, Guid PlayListId, Guid TrackId)
        : ICommand<ErrorOr<Success>>;
}
