using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Pictures;

internal class UpdateAlbumPictureActivity(
    IDatabase database,
    IPublishEndpoint publishEndpoint,
    ILogger<UpdateAlbumPictureActivity> logger)
    : IActivity<UpdateAlbumPictureArguments, UpdateAlbumPictureLog>
{
    public const string ExecuteEndpointName = "update-album-picture";

    public async Task<ExecutionResult> Execute(ExecuteContext<UpdateAlbumPictureArguments> executeContext)
    {
        var smallResizedVariable = executeContext.GetVariable<string>(executeContext.Arguments.SmallPictureVariable);
        ArgumentNullException.ThrowIfNull(smallResizedVariable, nameof(smallResizedVariable));
        var mediumResizedVariable = executeContext.GetVariable<string>(executeContext.Arguments.MediumPictureVariable);
        ArgumentNullException.ThrowIfNull(mediumResizedVariable, nameof(mediumResizedVariable));
        var largeResizedVariable = executeContext.GetVariable<string>(executeContext.Arguments.LargePictureVariable);
        ArgumentNullException.ThrowIfNull(largeResizedVariable, nameof(largeResizedVariable));

        try
        {
            var album = await database.Albums.FindAsync(
                [executeContext.Arguments.AlbumId],
                cancellationToken: executeContext.CancellationToken);

            if (album == null)
            {
                logger.LogWarning("Album {AlbumId} not found",
                    executeContext.Arguments.AlbumId);
                throw new InvalidOperationException($"Album with id {executeContext.Arguments.AlbumId} not found");
            }

            if (album.Pictures == null)
            {
                logger.LogWarning("Album {AlbumId} has no pending picture upload",
                    executeContext.Arguments.AlbumId);
                throw new InvalidOperationException($"Album with id {executeContext.Arguments.AlbumId} has no pending picture upload");
            }

            var log = new UpdateAlbumPictureLog(
                album.Id,
                album.Pictures.OriginalName,
                album.Pictures.SmallName,
                album.Pictures.MediumName,
                album.Pictures.LargeName);

            album.Pictures.OriginalName = Path.GetFileName(executeContext.Arguments.OriginalPictureKey);
            album.Pictures.SmallName = Path.GetFileName(smallResizedVariable);
            album.Pictures.MediumName = Path.GetFileName(mediumResizedVariable);
            album.Pictures.LargeName = Path.GetFileName(largeResizedVariable);

            database.Albums.Update(album);
            await database.SaveChangesAsync(executeContext.CancellationToken);

            logger.LogInformation("Updated album {AlbumId} pictures",
                executeContext.Arguments.AlbumId);

            await publishEndpoint.Publish(
                new AlbumPictureProcessed(executeContext.Arguments.AlbumId),
                executeContext.CancellationToken);

            return executeContext.Completed();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to update album {AlbumId} pictures",
                executeContext.Arguments.AlbumId);
            throw;
        }
    }

    public async Task<CompensationResult> Compensate(CompensateContext<UpdateAlbumPictureLog> compensateContext)
    {
        try
        {
            var album = await database.Albums.FindAsync(
                [compensateContext.Log.AlbumId],
                cancellationToken: compensateContext.CancellationToken);

            if (album == null)
            {
                return compensateContext.Compensated();
            }

            album.Pictures = new AlbumPictures
            {
                OriginalName = compensateContext.Log.PreviousOriginalPictureKey,
                SmallName = compensateContext.Log.PreviousSmallPictureKey,
                MediumName = compensateContext.Log.PreviousMediumPictureKey,
                LargeName = compensateContext.Log.PreviousLargePictureKey
            };

            database.Albums.Update(album);
            await database.SaveChangesAsync(compensateContext.CancellationToken);

            return compensateContext.Compensated();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to compensate album {AlbumId} pictures", compensateContext.Log.AlbumId);
            return compensateContext.Failed(exception);
        }
    }
}
