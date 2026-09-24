using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Albums;

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
        var album = await database.Albums
            .SingleOrDefaultAsync(a => a.Id == request.AlbumId && a.LifeCycleStatus == LifeCycleStatus.Active, cancellationToken);
        if (album == null)
            return AppErrors.NotFound("Album", request.AlbumId);

        if (album.OwnerUserId != request.UserId)
            return AppErrors.Forbidden("Album", request.AlbumId);

        album.LifeCycleStatus = LifeCycleStatus.Removing;

        await eventBus.PublishAsync(new DeleteAlbumEvent(album.Id, request.UserId), cancellationToken);
        await database.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Marked album {AlbumId} of user {UserId} as removing", request.AlbumId, request.UserId);

        return Result.Success;
    }
}
