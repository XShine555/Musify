using MassTransit;
using Musify.Application.Events;
using Musify.Application.Shared;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.Pictures;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    /// <summary>Downloads a picture, resizes it to the three sizes, uploads them and records the result on the owner.</summary>
    public class PictureWorkflowRoutingSlipBuilder(WorkerConfiguration workerConfiguration)
    {
        public RoutingSlipBuilder Build(UpdateTrackPictureEvent message, Guid? correlationId) =>
            BuildPictureWorkflow(
                message.TrackId, message.Bucket, message.SourceKey, message.Sizes, correlationId,
                ActivityNames.UpdateTrackPicture, UpdateTrackPictureActivity.ExecuteEndpointName,
                RoutingSlipVariableNames.ProcessKinds.TrackPicture);

        public RoutingSlipBuilder Build(UpdatePlayListPictureEvent message, Guid? correlationId) =>
            BuildPictureWorkflow(
                message.PlayListId, message.Bucket, message.SourceKey, message.Sizes, correlationId,
                ActivityNames.UpdatePlayListPicture, UpdatePlayListPictureActivity.ExecuteEndpointName,
                RoutingSlipVariableNames.ProcessKinds.PlayListPicture);

        public RoutingSlipBuilder Build(UpdateAlbumPictureEvent message, Guid? correlationId) =>
            BuildPictureWorkflow(
                message.AlbumId, message.Bucket, message.SourceKey, message.Sizes, correlationId,
                ActivityNames.UpdateAlbumPicture, UpdateAlbumPictureActivity.ExecuteEndpointName,
                RoutingSlipVariableNames.ProcessKinds.AlbumPicture);

        private RoutingSlipBuilder BuildPictureWorkflow(
            Guid subjectId,
            string bucket,
            string sourceKey,
            ImageSizes sizes,
            Guid? correlationId,
            string updateStepName,
            string updateEndpointName,
            string processKind)
        {
            var builder = RoutingSlips.Create(correlationId)
                .AddStep(
                    ActivityNames.GeneratePictureWorkflowPaths,
                    GeneratePictureWorkflowPathsActivity.ExecuteEndpointName,
                    new GeneratePictureWorkflowPathsArguments(workerConfiguration.Routes.TemporaryFilesDirectory, sourceKey))
                .AddStep(
                    ActivityNames.DownloadFile,
                    DownloadFileFromBucketActivity.ExecuteEndpointName,
                    new DownloadFileFromBucketArguments(bucket, sourceKey, RoutingSlipVariableNames.Picture.OriginalFilePath));

            AddResize(builder, ActivityNames.ResizeSmall, ActivityNames.UploadSmall, bucket, RoutingSlipVariableNames.Picture.SmallResizedFilePath, sizes.Small);
            AddResize(builder, ActivityNames.ResizeMedium, ActivityNames.UploadMedium, bucket, RoutingSlipVariableNames.Picture.MediumResizedFilePath, sizes.Medium);
            AddResize(builder, ActivityNames.ResizeLarge, ActivityNames.UploadLarge, bucket, RoutingSlipVariableNames.Picture.LargeResizedFilePath, sizes.Large);

            return builder
                .AddStep(
                    updateStepName,
                    updateEndpointName,
                    new UpdatePicturesArguments(
                        subjectId,
                        sourceKey,
                        RoutingSlipVariableNames.Picture.SmallResizedFilePath,
                        RoutingSlipVariableNames.Picture.MediumResizedFilePath,
                        RoutingSlipVariableNames.Picture.LargeResizedFilePath))
                .TrackFaults(subjectId, processKind);
        }

        private static void AddResize(
            RoutingSlipBuilder builder,
            string resizeStepName,
            string uploadStepName,
            string bucket,
            string resizedFileVariable,
            ImageSize size) =>
            builder
                .AddStep(
                    resizeStepName,
                    ResizePictureActivity.ExecuteEndpointName,
                    new ResizePictureLocalArguments(
                        RoutingSlipVariableNames.Picture.OriginalFilePath, resizedFileVariable, size.Width, size.Height))
                .AddStep(
                    uploadStepName,
                    UploadFileToBucketActivity.ExecuteEndpointName,
                    new UploadFileToBucketArguments(resizedFileVariable, bucket, size.SavePath));
    }
}
