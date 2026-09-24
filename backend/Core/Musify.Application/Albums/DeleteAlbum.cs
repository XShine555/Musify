using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Albums
{
    public record DeleteAlbumCommand(long UserId, Guid AlbumId)
        : ICommand<ErrorOr<Success>>;

    public class DeleteAlbumCommandHandler(
        IDatabase database,
        IEventBus eventBus,
        ILogger<DeleteAlbumCommandHandler> logger)
        : ICommandHandler<DeleteAlbumCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(DeleteAlbumCommand request, CancellationToken cancellationToken)
        {
            var found = await database.Albums.FindOwnedAsync(request.AlbumId, request.UserId, cancellationToken);
            if (found.IsError)
                return found.Errors;

            var album = found.Value;
            album.LifeCycleStatus = LifeCycleStatus.Removing;

            await eventBus.PublishAsync(new DeleteAlbumEvent(album.Id, request.UserId), cancellationToken);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Marked album {AlbumId} as removing", request.AlbumId);

            return Result.Success;
        }
    }
}
