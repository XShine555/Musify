using MassTransit;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class UpdatePlayListPictureActivity(IDatabase database)
        : IExecuteActivity<UpdatePlayListPictureArguments>
    {
        public const string ExecuteEndpointName = "Update-PlayList-Picture";

        public async Task<ExecutionResult> Execute(ExecuteContext<UpdatePlayListPictureArguments> executeContext)
        {
            var playList = await database.PlayLists.SingleOrDefaultAsync(p => p.Id == executeContext.Arguments.PlayListId,
                executeContext.CancellationToken);

            if (playList is null)
                throw new Exception($"PlayList with id {executeContext.Arguments.PlayListId} not found.");

            playList.SmallPictureKeyName = executeContext.Arguments.SmallPictureKeyName;
            playList.MediumPictureKeyName = executeContext.Arguments.MediumPictureKeyName;
            playList.LargePictureKeyName = executeContext.Arguments.LargePictureKeyName;

            database.PlayLists.Update(playList);
            await database.SaveChangesAsync(executeContext.CancellationToken);

            return executeContext.Completed();
        }
    }
}