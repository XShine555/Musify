using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists;

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
        var playList = await database.PlayLists.SingleOrDefaultAsync(p => p.Id == request.PlayListId, cancellationToken);
        if (playList == null)
        {
            logger.LogDebug("Playlist {PlayListId} not found", request.PlayListId);
            return Error.NotFound();
        }

        if (playList.UserId != request.UserId)
        {
            logger.LogWarning("Playlist {PlayListId} does not belong to user {UserId}", request.PlayListId, request.UserId);
            return Error.Unauthorized();
        }

        playList.LifeCycleStatus = LifeCycleStatus.Removing;

        try
        {
            await eventBus.PublishAsync(
                new DeletePlayListEvent(playList.Id, request.UserId),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish delete playlist event for playlist {PlayListId}", request.PlayListId);
            return Error.Failure(description: $"Failed to delete playlist {request.PlayListId}");
        }

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to mark playlist {PlayListId} as removing", request.PlayListId);
            return Error.Failure(description: $"Failed to delete playlist {request.PlayListId}");
        }

        return new Success();
    }
}
