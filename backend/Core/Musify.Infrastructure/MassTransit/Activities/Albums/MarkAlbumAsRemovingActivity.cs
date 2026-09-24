using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities.Albums;

internal class MarkAlbumAsRemovingActivity(
    IDatabase database,
    ILogger<MarkAlbumAsRemovingActivity> logger)
    : IExecuteActivity<MarkAlbumAsRemovingArguments>
{
    public const string ExecuteEndpointName = "mark-album-as-removing";

    public async Task<ExecutionResult> Execute(ExecuteContext<MarkAlbumAsRemovingArguments> executeContext)
    {
        var album = await database.Albums
            .SingleOrDefaultAsync(p => p.Id == executeContext.Arguments.AlbumId, executeContext.CancellationToken);

        if (album == null)
        {
            logger.LogInformation(
                "Album {AlbumId} not found while marking as removing, skipping",
                executeContext.Arguments.AlbumId);
            return executeContext.Completed();
        }

        album.LifeCycleStatus = LifeCycleStatus.Removing;
        await database.SaveChangesAsync(executeContext.CancellationToken);

        logger.LogInformation("Marked album {AlbumId} as removing", album.Id);
        return executeContext.Completed();
    }
}
