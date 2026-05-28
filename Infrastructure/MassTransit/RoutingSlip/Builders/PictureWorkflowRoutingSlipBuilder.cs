using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Common;
using Musify.Application.Events;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Activities;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    public class PictureWorkflowRoutingSlipBuilder(WorkerConfiguration workerConfiguration)
    {
        public RoutingSlipBuilder Build(UpdateTrackPictureEvent message, Guid? correlationId)
        {
            var routingSlipBuilder = BuildPictureWorkflow(
                message.Bucket,
                message.SourceKey,
                message.Small,
                message.Medium,
                message.Large,
                correlationId);

            routingSlipBuilder.AddActivity(
                ActivityNames.UpdateTrackPicture,
                EndpointHelper.BuildExecuteActivityUri(UpdateTrackPictureActivity.ExecuteEndpointName),
                new UpdateTrackPictureArguments(
                    message.TrackId,
                    message.SourceKey,
                    RoutingSlipVariableNames.Picture.SmallResizedFilePath,
                    RoutingSlipVariableNames.Picture.MediumResizedFilePath,
                    RoutingSlipVariableNames.Picture.LargeResizedFilePath));

            return routingSlipBuilder;
        }

        public RoutingSlipBuilder Build(UpdatePlayListPictureEvent message, Guid? correlationId)
        {
            var routingSlipBuilder = BuildPictureWorkflow(
                message.Bucket,
                message.SourceKey,
                message.Small,
                message.Medium,
                message.Large,
                correlationId);

            routingSlipBuilder.AddActivity(
                ActivityNames.UpdatePlayListPicture,
                EndpointHelper.BuildExecuteActivityUri(UpdatePlayListPictureActivity.ExecuteEndpointName),
                new UpdatePlayListPictureArguments(
                    message.PlayListId,
                    message.SourceKey,
                    RoutingSlipVariableNames.Picture.SmallResizedFilePath,
                    RoutingSlipVariableNames.Picture.MediumResizedFilePath,
                    RoutingSlipVariableNames.Picture.LargeResizedFilePath));

            return routingSlipBuilder;
        }

        RoutingSlipBuilder BuildPictureWorkflow(
            string bucket,
            string sourceKey,
            ImageSize small,
            ImageSize medium,
            ImageSize large,
            Guid? correlationId)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());
            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

            routingSlipBuilder.AddSubscription(
                EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            routingSlipBuilder.AddActivity(
                ActivityNames.GeneratePictureWorkflowPaths,
                EndpointHelper.BuildExecuteActivityUri(GeneratePictureWorkflowPathsActivity.ExecuteEndpointName),
                new GeneratePictureWorkflowPathsArguments(
                    workerConfiguration.Routes.TemporaryFilesDirectory,
                    sourceKey));

            routingSlipBuilder.AddActivity(
                ActivityNames.DownloadFile,
                EndpointHelper.BuildExecuteActivityUri(DownloadFileFromBucketActivity.ExecuteEndpointName),
                new DownloadFileFromBucketArguments(
                    bucket,
                    sourceKey,
                    RoutingSlipVariableNames.Picture.OriginalFilePath));

            AddResizeActivity(
                routingSlipBuilder,
                ActivityNames.ResizeSmall,
                ActivityNames.UploadSmall,
                bucket,
                RoutingSlipVariableNames.Picture.SmallResizedFilePath,
                small);

            AddResizeActivity(
                routingSlipBuilder,
                ActivityNames.ResizeMedium,
                ActivityNames.UploadMedium,
                bucket,
                RoutingSlipVariableNames.Picture.MediumResizedFilePath,
                medium);

            AddResizeActivity(
                routingSlipBuilder,
                ActivityNames.ResizeLarge,
                ActivityNames.UploadLarge,
                bucket,
                RoutingSlipVariableNames.Picture.LargeResizedFilePath,
                large);

            return routingSlipBuilder;
        }

        static void AddResizeActivity(
            RoutingSlipBuilder routingSlipBuilder,
            string resizeActivityName,
            string uploadActivityName,
            string bucketName,
            string destinationFilePathVariableName,
            ImageSize imageSize)
        {
            routingSlipBuilder.AddActivity(
                resizeActivityName,
                EndpointHelper.BuildExecuteActivityUri(ResizePictureActivity.ExecuteEndpointName),
                new ResizePictureLocalArguments(
                    RoutingSlipVariableNames.Picture.OriginalFilePath,
                    destinationFilePathVariableName,
                    imageSize.Width,
                    imageSize.Height));

            routingSlipBuilder.AddActivity(
                uploadActivityName,
                EndpointHelper.BuildExecuteActivityUri(UploadFileToBucketActivity.ExecuteEndpointName),
                new UploadFileToBucketArguments(
                    destinationFilePathVariableName,
                    bucketName,
                    imageSize.SavePath));
        }
    }
}