using MassTransit;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class ResizePictureActivity(IStorageHandler storageHandler, IPictureHandler pictureHandler)
        : IExecuteActivity<ResizePictureArgument>
    {
        public const string ExecuteEndpointName = "Resize-Picture";

        public async Task<ExecutionResult> Execute(ExecuteContext<ResizePictureArgument> executeContext)
        {
            var file = await storageHandler.GetFileAsync(
                executeContext.Arguments.OriginalBucketName,
                executeContext.Arguments.OriginalKeyName,
                executeContext.CancellationToken);

            if (!file.IsSuccess)
            {
                string errorMessages = string.Join(", ", file.Errors);
                throw new Exception(errorMessages);
            }

            var resizedPicture = await pictureHandler.ResizePictureAsync(
                file.Value,
                executeContext.Arguments.Width,
                executeContext.Arguments.Height,
                executeContext.CancellationToken);

            if (!resizedPicture.IsSuccess)
            {
                string errorMessage = string.Join(", ", resizedPicture.Errors);
                throw new Exception(errorMessage);
            }

            var uploadFile = await storageHandler.UploadFileAsync(
                resizedPicture.Value,
                pictureHandler.ContentType,
                executeContext.Arguments.DestinationBucketName,
                executeContext.Arguments.DestinationKeyName,
                executeContext.CancellationToken);

            if (!uploadFile.IsSuccess)
            {
                var errorMessage = string.Join(", ", uploadFile.Errors);
                throw new Exception(errorMessage);
            }

            return executeContext.Completed();
        }
    }
}