using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists
{
    public record DeletePlayListCommand(long UserId, Guid PlayListId)
        : ICommand<ErrorOr<Success>>;

    public class DeletePlayListCommandHandler(
        IDatabase database,
        IEventBus eventBus,
        ILogger<DeletePlayListCommandHandler> logger)
        : ICommandHandler<DeletePlayListCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(DeletePlayListCommand request, CancellationToken cancellationToken)
        {
            var found = await database.PlayLists.FindOwnedAsync(request.PlayListId, request.UserId, cancellationToken);
            if (found.IsError)
                return found.Errors;

            var playList = found.Value;
            playList.LifeCycleStatus = LifeCycleStatus.Removing;

            await eventBus.PublishAsync(new DeletePlayListEvent(playList.Id, request.UserId), cancellationToken);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Marked playlist {PlayListId} as removing", request.PlayListId);

            return Result.Success;
        }
    }
}
