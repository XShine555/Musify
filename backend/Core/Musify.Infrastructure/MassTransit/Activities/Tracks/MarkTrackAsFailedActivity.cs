using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities.Tracks
{
    internal class MarkTrackAsFailedActivity(
        IDatabase database,
        ILogger<MarkTrackAsFailedActivity> logger)
        : IExecuteActivity<MarkTrackAsFailedArguments>
    {
        public const string ExecuteEndpointName = "mark-track-as-failed";

        public async Task<ExecutionResult> Execute(ExecuteContext<MarkTrackAsFailedArguments> executeContext)
        {
            var track = await database.Tracks
                .SingleOrDefaultAsync(t => t.Id == executeContext.Arguments.TrackId, executeContext.CancellationToken);

            if (track is not null)
            {
                track.LifeCycleStatus = LifeCycleStatus.Failed;
                await database.SaveChangesAsync(executeContext.CancellationToken);
                logger.LogInformation("Marked track {TrackId} as failed", executeContext.Arguments.TrackId);
            }

            return executeContext.Completed();
        }
    }
}
