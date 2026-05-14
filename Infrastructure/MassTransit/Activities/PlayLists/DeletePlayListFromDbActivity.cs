using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;

namespace Musify.Infrastructure.MassTransit.Activities.PlayLists
{
    internal class DeletePlayListFromDbActivity(
        IDatabase database,
        ILogger<DeletePlayListFromDbActivity> logger)
        : IExecuteActivity<DeletePlayListFromDbArguments>
    {
        public const string ExecuteEndpointName = "delete-playList-from-db";

        public async Task<ExecutionResult> Execute(ExecuteContext<DeletePlayListFromDbArguments> executeContext)
        {
            var playList = await database.PlayLists
                .SingleOrDefaultAsync(p => p.Id == executeContext.Arguments.PlayListId, executeContext.CancellationToken);

            if (playList is null)
            {
                logger.LogInformation(
                    "PlayList {PlayListId} not found while deleting from DB, skipping",
                    executeContext.Arguments.PlayListId);
                return executeContext.Completed();
            }

            database.PlayLists.Remove(playList);
            await database.SaveChangesAsync(executeContext.CancellationToken);

            logger.LogInformation("Deleted playlist {PlayListId} from DB", playList.Id);
            return executeContext.Completed();
        }
    }
}
