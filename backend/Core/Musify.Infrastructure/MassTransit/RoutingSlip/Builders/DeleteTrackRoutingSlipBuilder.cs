using MassTransit;
using MassTransit.Courier.Contracts;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.Tracks;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;
using Musify.Application.Contracts;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    public class DeleteTrackRoutingSlipBuilder(
        IDatabase database,
        TrackConfiguration trackConfiguration,
        ApplicationStorageConfiguration storageConfiguration)
    {
        public async Task<RoutingSlipBuilder> BuildAsync(
            Guid trackId,
            long userId,
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

            if (track != null)
            {
                var bucket = storageConfiguration.Bucket;

                if (!string.IsNullOrEmpty(track.Pictures.OriginalName))
                {
                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemoveTrackOriginalPicture,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            trackConfiguration.Routes.BuildOriginalPicturePath(userId, track.Pictures.OriginalName)));
                }

                if (track.Pictures.IsProcessed)
                {
                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemoveTrackSmallPicture,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            trackConfiguration.Routes.BuildSmallPicturePath(track.Pictures.SmallName)));

                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemoveTrackMediumPicture,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            trackConfiguration.Routes.BuildMediumPicturePath(track.Pictures.MediumName)));

                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemoveTrackLargePicture,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            trackConfiguration.Routes.BuildLargePicturePath(track.Pictures.LargeName)));
                }

                if (!string.IsNullOrEmpty(track.Audio.OriginalName))
                {
                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemoveTrackOriginalAudio,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            trackConfiguration.Routes.BuildOriginalAudioPath(userId, track.Audio.OriginalName)));
                }

                if (track.Audio.IsProcessed)
                {
                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemoveTrackProcessedAudio,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            trackConfiguration.Routes.BuildProcessedAudioPath(track.Audio.FolderName)));
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
