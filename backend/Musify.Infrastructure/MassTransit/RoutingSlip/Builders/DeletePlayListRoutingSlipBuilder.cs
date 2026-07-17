using MassTransit;
using MassTransit.Courier.Contracts;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.PlayLists;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;
using Musify.Application.Contracts;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    public class DeletePlayListRoutingSlipBuilder(
        IDatabase database,
        PlayListConfiguration playListConfiguration,
        ApplicationStorageConfiguration storageConfiguration)
    {
        public async Task<RoutingSlipBuilder> BuildAsync(
            Guid playListId,
            long userId,
            Guid? correlationId,
            CancellationToken cancellationToken)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());
            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

            routingSlipBuilder.AddSubscription(
                EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            var playList = await database.PlayLists
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == playListId, cancellationToken);

            routingSlipBuilder.AddActivity(
                ActivityNames.MarkPlayListAsRemoving,
                EndpointHelper.BuildExecuteActivityUri(MarkPlayListAsRemovingActivity.ExecuteEndpointName),
                new MarkPlayListAsRemovingArguments(playListId));

            if (playList is not null)
            {
                var bucket = storageConfiguration.Bucket;
                var routes = playListConfiguration.Routes;

                if (playList.Pictures.OriginalName != routes.PresetOriginalPicture)
                {
                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemovePlayListOriginalPicture,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            routes.BuildOriginalPicturePath(userId, playList.Pictures.OriginalName)));
                }

                if (playList.Pictures.SmallName != routes.PresetSmallPicture)
                {
                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemovePlayListSmallPicture,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            routes.BuildSmallPicturePath(playList.Pictures.SmallName)));
                }

                if (playList.Pictures.MediumName != routes.PresetMediumPicture)
                {
                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemovePlayListMediumPicture,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            routes.BuildMediumPicturePath(playList.Pictures.MediumName)));
                }

                if (playList.Pictures.LargeName != routes.PresetLargePicture)
                {
                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemovePlayListLargePicture,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            routes.BuildLargePicturePath(playList.Pictures.LargeName)));
                }
            }

            routingSlipBuilder.AddActivity(
                ActivityNames.DeletePlayListFromDb,
                EndpointHelper.BuildExecuteActivityUri(DeletePlayListFromDbActivity.ExecuteEndpointName),
                new DeletePlayListFromDbArguments(playListId));

            return routingSlipBuilder;
        }
    }
}
