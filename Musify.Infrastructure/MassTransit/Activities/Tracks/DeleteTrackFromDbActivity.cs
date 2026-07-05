using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities.Tracks
{
    internal class DeleteTrackFromDbActivity(
        IDatabase database,
        ILogger<DeleteTrackFromDbActivity> logger)
        : IExecuteActivity<DeleteTrackFromDbArguments>
    {
        public const string ExecuteEndpointName = "delete-track-from-db";

        public async Task<ExecutionResult> Execute(ExecuteContext<DeleteTrackFromDbArguments> executeContext)
        {
            var track = await database.Tracks
                .SingleOrDefaultAsync(t => t.Id == executeContext.Arguments.TrackId, executeContext.CancellationToken);

            if (track is null)
            {
                logger.LogInformation(
                    "Track {TrackId} not found while deleting from DB, skipping",
                    executeContext.Arguments.TrackId);
                return executeContext.Completed();
            }

            database.Tracks.Remove(track);
            await database.SaveChangesAsync(executeContext.CancellationToken);

            logger.LogInformation("Deleted track {TrackId} from DB", track.Id);
            return executeContext.Completed();
        }
    }
}
