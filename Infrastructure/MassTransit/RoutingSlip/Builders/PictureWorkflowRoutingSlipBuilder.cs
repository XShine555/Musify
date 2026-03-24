using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;
using Musify.Infrastructure.Messaging.Consumers;

namespace Musify.Infrastructure.Messaging.RoutingSlip.Builders
{
    public class PictureWorkflowRoutingSlipBuilder(WorkerConfiguration workerConfiguration)
    {
        public RoutingSlipBuilder Build(UpdateTrackPictureEvent message, Guid? correlationId)
        {
            var routingSlipBuilder = BuildPictureWorkflow(
                message.SourceBucketName,
                message.SourceKeyName,
                message.SmallPictureKeyName,
                message.SmallPictureWidth,
                message.SmallPictureHeight,
                message.MediumPictureKeyName,
                message.MediumPictureWidth,
                message.MediumPictureHeight,
                message.LargePictureKeyName,
                message.LargePictureWidth,
                message.LargePictureHeight,
                correlationId);

            routingSlipBuilder.AddActivity(
                ActivityNames.UpdateTrackPicture,
                MessagingHelper.BuildExecuteActivityUri(UpdateTrackPictureActivity.ExecuteEndpointName),
                new UpdateTrackPictureArguments(
                    message.TrackId,
                    message.SourceKeyName,
                    message.SmallPictureKeyName,
                    message.MediumPictureKeyName,
                    message.LargePictureKeyName));

            return routingSlipBuilder;
        }

        public RoutingSlipBuilder Build(UpdatePlayListPictureEvent message, Guid? correlationId)
        {
            var routingSlipBuilder = BuildPictureWorkflow(
                message.SourceBucketName,
                message.SourceKeyName,
                message.SmallPictureKeyName,
                message.SmallPictureWidth,
                message.SmallPictureHeight,
                message.MediumPictureKeyName,
                message.MediumPictureWidth,
                message.MediumPictureHeight,
                message.LargePictureKeyName,
                message.LargePictureWidth,
                message.LargePictureHeight,
                correlationId);

            routingSlipBuilder.AddActivity(
                ActivityNames.UpdatePlayListPicture,
                MessagingHelper.BuildExecuteActivityUri(UpdatePlayListPictureActivity.ExecuteEndpointName),
                new UpdatePlayListPictureArguments(
                    message.PlayListId,
                    message.SourceKeyName,
                    message.SmallPictureKeyName,
                    message.MediumPictureKeyName,
                    message.LargePictureKeyName));

            return routingSlipBuilder;
        }

        RoutingSlipBuilder BuildPictureWorkflow(
            string sourceBucketName,
            string sourceKeyName,
            string smallPictureKeyName,
            int smallPictureWidth,
            int smallPictureHeight,
            string mediumPictureKeyName,
            int mediumPictureWidth,
            int mediumPictureHeight,
            string largePictureKeyName,
            int largePictureWidth,
            int largePictureHeight,
            Guid? correlationId)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());

            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

            routingSlipBuilder.AddActivity(
                ActivityNames.GeneratePictureWorkflowPaths,
                MessagingHelper.BuildExecuteActivityUri(GeneratePictureWorkflowPathsActivity.ExecuteEndpointName),
                new GeneratePictureWorkflowPathsArguments(
                    workerConfiguration.Routes.TemporaryFilesDirectory,
                    sourceKeyName,
                    smallPictureKeyName,
                    mediumPictureKeyName,
                    largePictureKeyName));

            routingSlipBuilder.AddActivity(
                ActivityNames.DownloadFile,
                MessagingHelper.BuildExecuteActivityUri(DownloadFileFromBucketActivity.ExecuteEndpointName),
                new DownloadFileFromBucketArguments(
                    sourceBucketName,
                    sourceKeyName,
                    RoutingSlipVariableNames.Picture.OriginalFilePath));

            AddResizeActivity(
                routingSlipBuilder,
                ActivityNames.ResizeSmall,
                RoutingSlipVariableNames.Picture.SmallResizedFilePath,
                smallPictureWidth,
                smallPictureHeight);

            AddResizeActivity(
                routingSlipBuilder,
                ActivityNames.ResizeMedium,
                RoutingSlipVariableNames.Picture.MediumResizedFilePath,
                mediumPictureWidth,
                mediumPictureHeight);

            AddResizeActivity(
                routingSlipBuilder,
                ActivityNames.ResizeLarge,
                RoutingSlipVariableNames.Picture.LargeResizedFilePath,
                largePictureWidth,
                largePictureHeight);

            routingSlipBuilder.AddActivity(
                ActivityNames.DeleteOriginal,
                MessagingHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                new RemoveFileFromBucketArguments(sourceBucketName, sourceKeyName));

            routingSlipBuilder.AddActivity(
                ActivityNames.UploadFiles,
                MessagingHelper.BuildExecuteActivityUri(TransferFilesToBucket.ExecuteEndpointName),
                new TransferFilesToBucketArguments(
                    sourceBucketName,
                    RoutingSlipVariableNames.Workflow.TempDirectory,
                    DestinationKeyNameVariableName: RoutingSlipVariableNames.Picture.DestinationFolderName));

            return routingSlipBuilder;
        }

        static void AddResizeActivity(
            RoutingSlipBuilder routingSlipBuilder,
            string activityName,
            string destinationFilePathVariableName,
            int width,
            int height)
        {
            routingSlipBuilder.AddActivity(
                activityName,
                MessagingHelper.BuildExecuteActivityUri(ResizePictureActivity.ExecuteEndpointName),
                new ResizePictureLocalArguments(
                    RoutingSlipVariableNames.Picture.OriginalFilePath,
                    destinationFilePathVariableName,
                    width,
                    height));
        }
    }
}
