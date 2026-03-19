using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;

namespace Musify.Application.PlayLists.Commands.DeletePlayList
{
    public class DeletePlayListCommandHandler(IDatabase database, IStorageHandler storageHandler,
        StorageConfiguration storageConfiguration, ILogger<DeletePlayListCommandHandler> logger)
        : IRequestHandler<DeletePlayListCommand, Task<Result>>
    {
        public async Task<Result> Handle(DeletePlayListCommand request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists.FindAsync(request.PlayListId, cancellationToken);

            if (playList is null)
            {
                logger.LogDebug("PlayList with id={PlayListId} not found", request.PlayListId);
                return Result.NotFound();
            }

            if (playList.UserId != request.UserId)
            {
                logger.LogInformation("PlayList with id={PlayListId} does not belong to user with id={UserId}", request.PlayListId, request.UserId);
                return Result.Unauthorized();
            }

            database.PlayLists.Remove(playList);

            await database.SaveChangesAsync(cancellationToken);

            return Result.NoContent();
        }
    }
}
