using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.UploadIntents;

internal class ConsumeUploadIntentsActivity(
    IDatabase database,
    ILogger<ConsumeUploadIntentsActivity> logger)
    : IActivity<ConsumeUploadIntentsArguments, ConsumeUploadIntentsLog>
{
    public const string ExecuteEndpointName = "consume-upload-intents";

    public async Task<ExecutionResult> Execute(ExecuteContext<ConsumeUploadIntentsArguments> executeContext)
    {
        var intentIds = executeContext.Arguments.IntentIds;

        var intents = await database.UploadIntents
            .Where(i => intentIds.Contains(i.Id))
            .ToListAsync(executeContext.CancellationToken);

        foreach (var intent in intents)
            intent.Status = UploadIntentStatus.Consumed;

        await database.SaveChangesAsync(executeContext.CancellationToken);

        logger.LogInformation("Marked {Count} upload intent(s) as consumed", intents.Count);

        return executeContext.Completed(new ConsumeUploadIntentsLog(intentIds));
    }

    public async Task<CompensationResult> Compensate(CompensateContext<ConsumeUploadIntentsLog> compensateContext)
    {
        try
        {
            var intentIds = compensateContext.Log.IntentIds;

            var intents = await database.UploadIntents
                .Where(i => intentIds.Contains(i.Id))
                .ToListAsync(compensateContext.CancellationToken);

            foreach (var intent in intents)
                intent.Status = UploadIntentStatus.Issued;

            await database.SaveChangesAsync(compensateContext.CancellationToken);

            logger.LogInformation("Reverted {Count} upload intent(s) back to Issued", intents.Count);

            return compensateContext.Compensated();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to compensate upload intent consumption");
            return compensateContext.Failed(exception);
        }
    }
}
