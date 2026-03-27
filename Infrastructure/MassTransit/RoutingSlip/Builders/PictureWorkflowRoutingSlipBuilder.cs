using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Activities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;
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
                message.SourceBucketName,
                message.SourceKeyName,
                message.SmallPictureRoute,
                message.SmallPictureWidth,
                message.SmallPictureHeight,
                message.MediumPictureRoute,
                message.MediumPictureWidth,
                message.MediumPictureHeight,
                message.LargePictureRoute,
                message.LargePictureWidth,
                message.LargePictureHeight,
                correlationId);

            routingSlipBuilder.AddActivity(
                ActivityNames.UpdateTrackPicture,
                EndpointHelper.BuildExecuteActivityUri(UpdateTrackPictureActivity.ExecuteEndpointName),
                new UpdateTrackPictureArguments(
                    message.TrackId,
                    message.SourceKeyName,
                    RoutingSlipVariableNames.Picture.SmallResizedFilePath,
                    RoutingSlipVariableNames.Picture.MediumResizedFilePath,
                    RoutingSlipVariableNames.Picture.LargeResizedFilePath));

            return routingSlipBuilder;
        }

        public RoutingSlipBuilder Build(UpdatePlayListPictureEvent message, Guid? correlationId)
        {
            var routingSlipBuilder = BuildPictureWorkflow(
                message.SourceBucketName,
                message.SourceKeyName,
                message.SmallPictureRoute,
                message.SmallPictureWidth,
                message.SmallPictureHeight,
                message.MediumPictureRoute,
                message.MediumPictureWidth,
                message.MediumPictureHeight,
                message.LargePictureRoute,
                message.LargePictureWidth,
                message.LargePictureHeight,
                correlationId);

            routingSlipBuilder.AddActivity(
                ActivityNames.UpdatePlayListPicture,
                EndpointHelper.BuildExecuteActivityUri(UpdatePlayListPictureActivity.ExecuteEndpointName),
                new UpdatePlayListPictureArguments(
                    message.PlayListId,
                    message.SourceKeyName,
                    RoutingSlipVariableNames.Picture.SmallResizedFilePath,
                    RoutingSlipVariableNames.Picture.MediumResizedFilePath,
                    RoutingSlipVariableNames.Picture.LargeResizedFilePath));

            return routingSlipBuilder;
        }

        RoutingSlipBuilder BuildPictureWorkflow(
            string bucketName,
            string sourceKeyName,
            string destinationSmallPictureRoute,
            int smallPictureWidth,
            int smallPictureHeight,
            string destinationMediumPictureRoute,
            int mediumPictureWidth,
            int mediumPictureHeight,
            string destinationLargePictureRoute,
            int largePictureWidth,
            int largePictureHeight,
            Guid? correlationId)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());

            routingSlipBuilder.AddSubscription(
                EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

            routingSlipBuilder.AddActivity(
                ActivityNames.GeneratePictureWorkflowPaths,
                EndpointHelper.BuildExecuteActivityUri(GeneratePictureWorkflowPathsActivity.ExecuteEndpointName),
                new GeneratePictureWorkflowPathsArguments(
                    workerConfiguration.Routes.TemporaryFilesDirectory,
                    sourceKeyName));

            routingSlipBuilder.AddActivity(
                ActivityNames.DownloadFile,
                EndpointHelper.BuildExecuteActivityUri(DownloadFileFromBucketActivity.ExecuteEndpointName),
                new DownloadFileFromBucketArguments(
                    bucketName,
                    sourceKeyName,
                    RoutingSlipVariableNames.Picture.OriginalFilePath));

            AddResizeActivity(
                routingSlipBuilder,
                ActivityNames.ResizeSmall,
                ActivityNames.UploadSmall,
                bucketName,
                RoutingSlipVariableNames.Picture.SmallResizedFilePath,
                destinationSmallPictureRoute,
                smallPictureWidth,
                smallPictureHeight);

            AddResizeActivity(
                routingSlipBuilder,
                ActivityNames.ResizeMedium,
                ActivityNames.ResizeMedium,
                bucketName,
                RoutingSlipVariableNames.Picture.MediumResizedFilePath,
                destinationMediumPictureRoute,
                mediumPictureWidth,
                mediumPictureHeight);

            AddResizeActivity(
                routingSlipBuilder,
                ActivityNames.ResizeLarge,
                ActivityNames.UploadLarge,
                bucketName,
                RoutingSlipVariableNames.Picture.LargeResizedFilePath,
                destinationLargePictureRoute,
                largePictureWidth,
                largePictureHeight);

            return routingSlipBuilder;
        }

        static void AddResizeActivity(
            RoutingSlipBuilder routingSlipBuilder,
            string resizeActivityName,
            string uploadActivityName,
            string bucketName,
            string destinationFilePathVariableName,
            string destinationBucketRoute,
            int width,
            int height)
        {
            routingSlipBuilder.AddActivity(
                resizeActivityName,
                EndpointHelper.BuildExecuteActivityUri(ResizePictureActivity.ExecuteEndpointName),
                new ResizePictureLocalArguments(
                    RoutingSlipVariableNames.Picture.OriginalFilePath,
                    destinationFilePathVariableName,
                    width,
                    height));

            routingSlipBuilder.AddActivity(
                uploadActivityName,
                EndpointHelper.BuildExecuteActivityUri(UploadFileToBucketActivity.ExecuteEndpointName),
                new UploadFileToBucketArguments(
                    destinationFilePathVariableName,
                    bucketName,
                    destinationBucketRoute));
        }
    }
}
