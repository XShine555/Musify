using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Events;
using Musify.Application.PlayLists.Commands;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Handlers
{
    public class DeletePlayListCommandHandler(
        IDatabase database,
        IEventBus eventBus,
        ILogger<DeletePlayListCommandHandler> logger)
        : ICommandHandler<DeletePlayListCommand, Result>
    {
        public async ValueTask<Result> Handle(DeletePlayListCommand request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists.SingleOrDefaultAsync(p => p.Id == request.PlayListId, cancellationToken);
            if (playList is null)
            {
                logger.LogDebug("Playlist {PlayListId} not found", request.PlayListId);
                return Result.NotFound();
            }

            if (playList.UserId != request.UserId)
            {
                logger.LogWarning("Playlist {PlayListId} does not belong to user {UserId}", request.PlayListId, request.UserId);
                return Result.Unauthorized();
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
                return Result.Error($"Failed to delete playlist {request.PlayListId}");
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to mark playlist {PlayListId} as removing", request.PlayListId);
                return Result.Error($"Failed to delete playlist {request.PlayListId}");
            }

            return Result.NoContent();
        }
    }
}