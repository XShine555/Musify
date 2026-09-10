using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities.Albums
{
    internal class MarkAlbumAsFailedActivity(
        IDatabase database,
        ILogger<MarkAlbumAsFailedActivity> logger)
        : IExecuteActivity<MarkAlbumAsFailedArguments>
    {
        public const string ExecuteEndpointName = "mark-album-as-failed";

        public async Task<ExecutionResult> Execute(ExecuteContext<MarkAlbumAsFailedArguments> executeContext)
        {
            var album = await database.Albums
                .SingleOrDefaultAsync(a => a.Id == executeContext.Arguments.AlbumId, executeContext.CancellationToken);

            if (album is not null)
            {
                album.LifeCycleStatus = LifeCycleStatus.Failed;
                await database.SaveChangesAsync(executeContext.CancellationToken);
                logger.LogInformation("Marked album {AlbumId} as failed", executeContext.Arguments.AlbumId);
            }

            return executeContext.Completed();
        }
    }
}
