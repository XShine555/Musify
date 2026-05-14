using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Domain.Entities;

namespace Musify.Infrastructure.MassTransit.Activities.PlayLists
{
    internal class MarkPlayListAsRemovingActivity(
        IDatabase database,
        ILogger<MarkPlayListAsRemovingActivity> logger)
        : IExecuteActivity<MarkPlayListAsRemovingArguments>
    {
        public const string ExecuteEndpointName = "mark-playList-as-removing";

        public async Task<ExecutionResult> Execute(ExecuteContext<MarkPlayListAsRemovingArguments> executeContext)
        {
            var playList = await database.PlayLists
                .SingleOrDefaultAsync(p => p.Id == executeContext.Arguments.PlayListId, executeContext.CancellationToken);

            if (playList is null)
            {
                logger.LogInformation(
                    "PlayList {PlayListId} not found while marking as removing, skipping",
                    executeContext.Arguments.PlayListId);
                return executeContext.Completed();
            }

            playList.LifeCycleStatus = LifeCycleStatus.Removing;
            await database.SaveChangesAsync(executeContext.CancellationToken);

            logger.LogInformation("Marked playlist {PlayListId} as removing", playList.Id);
            return executeContext.Completed();
        }
    }
}
