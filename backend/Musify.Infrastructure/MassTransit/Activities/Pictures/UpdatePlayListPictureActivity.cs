using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Pictures
{
    internal class UpdatePlayListPictureActivity(
        IDatabase database,
        IPublishEndpoint publishEndpoint,
        ILogger<UpdatePlayListPictureActivity> logger)
        : IActivity<UpdatePlayListPictureArguments, UpdatePlayListPictureLog>
    {
        public const string ExecuteEndpointName = "update-play-list-picture";

        public async Task<ExecutionResult> Execute(ExecuteContext<UpdatePlayListPictureArguments> executeContext)
        {
            var smallResizedVariable = executeContext.GetVariable<string>(executeContext.Arguments.SmallPictureVariable);
            ArgumentNullException.ThrowIfNull(smallResizedVariable, nameof(smallResizedVariable));
            var mediumResizedVariable = executeContext.GetVariable<string>(executeContext.Arguments.MediumPictureVariable);
            ArgumentNullException.ThrowIfNull(mediumResizedVariable, nameof(mediumResizedVariable));
            var largeResizedVariable = executeContext.GetVariable<string>(executeContext.Arguments.LargePictureVariable);
            ArgumentNullException.ThrowIfNull(largeResizedVariable, nameof(largeResizedVariable));

            try
            {
                var playList = await database.PlayLists.FindAsync(
                    [executeContext.Arguments.PlayListId],
                    cancellationToken: executeContext.CancellationToken);

                if (playList is null)
                {
                    logger.LogWarning("Playlist {PlayListId} not found",
                        executeContext.Arguments.PlayListId);
                    throw new InvalidOperationException($"PlayList with id {executeContext.Arguments.PlayListId} not found");
                }

                var log = new UpdatePlayListPictureLog(
                    playList.Id,
                    playList.OriginalPictureName,
                    playList.SmallPictureName,
                    playList.MediumPictureName,
                    playList.LargePictureName);

                playList.OriginalPictureName = Path.GetFileName(executeContext.Arguments.OriginalPictureKey);
                playList.SmallPictureName = Path.GetFileName(smallResizedVariable);
                playList.MediumPictureName = Path.GetFileName(mediumResizedVariable);
                playList.LargePictureName = Path.GetFileName(largeResizedVariable);

                database.PlayLists.Update(playList);
                await database.SaveChangesAsync(executeContext.CancellationToken);

                logger.LogInformation("Updated playlist {PlayListId} pictures",
                    executeContext.Arguments.PlayListId);

                await publishEndpoint.Publish(
                    new PlayListPictureProcessed(executeContext.Arguments.PlayListId),
                    executeContext.CancellationToken);

                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to update playlist {PlayListId} pictures",
                    executeContext.Arguments.PlayListId);
                throw;
            }
        }

        public async Task<CompensationResult> Compensate(CompensateContext<UpdatePlayListPictureLog> compensateContext)
        {
            try
            {
                var playList = await database.PlayLists.FindAsync(
                    [compensateContext.Log.PlayListId],
                    cancellationToken: compensateContext.CancellationToken);

                if (playList is null)
                {
                    return compensateContext.Compensated();
                }

                playList.OriginalPictureName = compensateContext.Log.PreviousOriginalPictureKey;
                playList.SmallPictureName = compensateContext.Log.PreviousSmallPictureKey;
                playList.MediumPictureName = compensateContext.Log.PreviousMediumPictureKey;
                playList.LargePictureName = compensateContext.Log.PreviousLargePictureKey;

                database.PlayLists.Update(playList);
                await database.SaveChangesAsync(compensateContext.CancellationToken);

                return compensateContext.Compensated();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to compensate playlist {PlayListId} pictures", compensateContext.Log.PlayListId);
                return compensateContext.Failed(exception);
            }
        }
    }
}