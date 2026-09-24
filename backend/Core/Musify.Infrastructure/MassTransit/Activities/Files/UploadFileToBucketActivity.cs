using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Files
{
    internal class UploadFileToBucketActivity(IStorageService storageService,
        ILogger<UploadFileToBucketActivity> logger)
        : IActivity<UploadFileToBucketArguments, UploadFileToBucketLog>
    {
        public const string ExecuteEndpointName = "upload-file-to-bucket";

        public async Task<ExecutionResult> Execute(ExecuteContext<UploadFileToBucketArguments> executeContext)
        {
            var sourceFilePath = executeContext.GetVariable<string>(executeContext.Arguments.FilePathVariable);
            ArgumentNullException.ThrowIfNull(sourceFilePath);
            var fileName = Path.GetFileName(sourceFilePath);
            var destinationKey = string.Join('/', new[] { executeContext.Arguments.DestinationRoute, fileName }
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .Select(static s => s.Trim().Trim('/', '\\')));

            await storageService.UploadFileAsync(
                sourceFilePath,
                executeContext.Arguments.DestinationBucket,
                destinationKey,
                executeContext.CancellationToken);
            return executeContext.CompletedWithVariables(new UploadFileToBucketLog(
                executeContext.Arguments.DestinationBucket,
                destinationKey));
        }

        public async Task<CompensationResult> Compensate(CompensateContext<UploadFileToBucketLog> compensateContext)
        {
            try
            {
                await storageService.RemoveFileAsync(
                    compensateContext.Log.DestinationBucket,
                    compensateContext.Log.DestinationKey,
                    compensateContext.CancellationToken);
                return compensateContext.Compensated();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error compensating file upload for {Bucket}/{Key}",
                    compensateContext.Log.DestinationBucket,
                    compensateContext.Log.DestinationKey);
                return compensateContext.Failed(exception);
            }
        }
    }
}
