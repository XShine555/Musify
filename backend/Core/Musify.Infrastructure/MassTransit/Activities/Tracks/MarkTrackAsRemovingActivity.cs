using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Domain.ValueObjects;
using Musify.Application.Contracts;

namespace Musify.Infrastructure.MassTransit.Activities.Tracks
{
    internal class MarkTrackAsRemovingActivity(
        IDatabase database,
        ILogger<MarkTrackAsRemovingActivity> logger)
        : IExecuteActivity<MarkTrackAsRemovingArguments>
    {
        public const string ExecuteEndpointName = "mark-track-as-removing";

        public async Task<ExecutionResult> Execute(ExecuteContext<MarkTrackAsRemovingArguments> executeContext)
        {
            var track = await database.Tracks
                .SingleOrDefaultAsync(t => t.Id == executeContext.Arguments.TrackId, executeContext.CancellationToken);

            if (track == null)
            {
                logger.LogInformation(
                    "Track {TrackId} not found while marking as removing, skipping",
                    executeContext.Arguments.TrackId);
                return executeContext.Completed();
            }

            track.LifeCycleStatus = LifeCycleStatus.Removing;
            await database.SaveChangesAsync(executeContext.CancellationToken);

            logger.LogInformation("Marked track {TrackId} as removing", track.Id);
            return executeContext.Completed();
        }
    }
}
