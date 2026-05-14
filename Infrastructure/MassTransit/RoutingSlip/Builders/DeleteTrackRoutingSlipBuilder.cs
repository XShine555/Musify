using MassTransit;
using MassTransit.Courier.Contracts;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Configuration;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.Arguments;
using Musify.Infrastructure.MassTransit.Activities.Tracks;
using Musify.Infrastructure.MassTransit.Consumers;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    public class DeleteTrackRoutingSlipBuilder(
        IDatabase database,
        TrackConfiguration trackConfiguration,
        ApplicationStorageConfiguration storageConfiguration)
    {
        public async Task<RoutingSlipBuilder> BuildAsync(
            Guid trackId,
            Guid userId,
            Guid? correlationId,
            CancellationToken cancellationToken)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());
            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

            routingSlipBuilder.AddSubscription(
                EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            var track = await database.Tracks
                .AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == trackId, cancellationToken);

            routingSlipBuilder.AddActivity(
                ActivityNames.MarkTrackAsRemoving,
                EndpointHelper.BuildExecuteActivityUri(MarkTrackAsRemovingActivity.ExecuteEndpointName),
                new MarkTrackAsRemovingArguments(trackId));

            if (track is not null)
            {
                var bucket = storageConfiguration.Bucket;

                routingSlipBuilder.AddActivity(
                    "RemoveTrackOriginalPicture",
                    EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                    new RemoveFileFromBucketArguments(
                        bucket,
                        trackConfiguration.Routes.BuildOriginalPicturePath(userId, track.OriginalPictureName)));

                if (track.IsPicturesProcessed)
                {
                    routingSlipBuilder.AddActivity(
                        "RemoveTrackSmallPicture",
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            trackConfiguration.Routes.BuildSmallPicturePath(track.SmallPictureName)));

                    routingSlipBuilder.AddActivity(
                        "RemoveTrackMediumPicture",
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            trackConfiguration.Routes.BuildMediumPicturePath(track.MediumPictureName)));

                    routingSlipBuilder.AddActivity(
                        "RemoveTrackLargePicture",
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            trackConfiguration.Routes.BuildLargePicturePath(track.LargePictureName)));
                }

                routingSlipBuilder.AddActivity(
                    "RemoveTrackOriginalAudio",
                    EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                    new RemoveFileFromBucketArguments(
                        bucket,
                        trackConfiguration.Routes.BuildOriginalAudioPath(userId, track.OriginalAudioName)));

                if (track.IsAudioProcessed)
                {
                    routingSlipBuilder.AddActivity(
                        "RemoveTrackProcessedAudio",
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            trackConfiguration.Routes.BuildProcessedAudioPath(track.AudioFolderName)));
                }
            }

            routingSlipBuilder.AddActivity(
                ActivityNames.DeleteTrackFromDb,
                EndpointHelper.BuildExecuteActivityUri(DeleteTrackFromDbActivity.ExecuteEndpointName),
                new DeleteTrackFromDbArguments(trackId));

            return routingSlipBuilder;
        }
    }
}
