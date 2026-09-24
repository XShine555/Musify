using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities.Albums;

internal class DeleteAlbumFromDbActivity(
    IDatabase database,
    ILogger<DeleteAlbumFromDbActivity> logger)
    : IExecuteActivity<DeleteAlbumFromDbArguments>
{
    public const string ExecuteEndpointName = "delete-album-from-db";

    public async Task<ExecutionResult> Execute(ExecuteContext<DeleteAlbumFromDbArguments> executeContext)
    {
        var album = await database.Albums
            .SingleOrDefaultAsync(p => p.Id == executeContext.Arguments.AlbumId, executeContext.CancellationToken);

        if (album == null)
        {
            logger.LogInformation(
                "Album {AlbumId} not found while deleting from DB, skipping",
                executeContext.Arguments.AlbumId);
            return executeContext.Completed();
        }

        database.Albums.Remove(album);
        await database.SaveChangesAsync(executeContext.CancellationToken);

        logger.LogInformation("Deleted album {AlbumId} from DB", album.Id);
        return executeContext.Completed();
    }
}
