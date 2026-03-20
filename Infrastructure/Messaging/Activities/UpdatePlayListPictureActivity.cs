using MassTransit;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class UpdatePlayListPictureActivity(IDatabase database)
        : IExecuteActivity<UpdatePlayListPictureArguments>
    {
        public async Task<ExecutionResult> Execute(ExecuteContext<UpdatePlayListPictureArguments> executeContext)
        {
            var PlayList = await database.PlayLists.FindAsync(executeContext.Arguments.PlayListId);

            if (PlayList is null)
            {
                throw new Exception($"PlayList with id {executeContext.Arguments.PlayListId} not found.");
            }

            PlayList.SmallPictureKeyName = executeContext.Arguments.SmallPictureKeyName;
            PlayList.MediumPictureKeyName = executeContext.Arguments.MediumPictureKeyName;
            PlayList.LargePictureKeyName = executeContext.Arguments.LargePictureKeyName;

            database.PlayLists.Update(PlayList);
            await database.SaveChangesAsync(executeContext.CancellationToken);

            return executeContext.Completed();
        }
    }
}