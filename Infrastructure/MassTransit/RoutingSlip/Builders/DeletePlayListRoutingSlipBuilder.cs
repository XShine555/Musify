using MassTransit;
using MassTransit.Courier.Contracts;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Configuration;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.Arguments;
using Musify.Infrastructure.MassTransit.Activities.PlayLists;
using Musify.Infrastructure.MassTransit.Consumers;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    public class DeletePlayListRoutingSlipBuilder(
        IDatabase database,
        PlayListConfiguration playListConfiguration,
        ApplicationStorageConfiguration storageConfiguration)
    {
        public async Task<RoutingSlipBuilder> BuildAsync(
            Guid playListId,
            Guid userId,
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

                if (playList.OriginalPictureName != routes.PresetOriginalPicture)
                {
                    routingSlipBuilder.AddActivity(
                        "RemovePlayListOriginalPicture",
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            routes.BuildOriginalPicturePath(userId, playList.OriginalPictureName)));
                }

                if (playList.SmallPictureName != routes.PresetSmallPicture)
                {
                    routingSlipBuilder.AddActivity(
                        "RemovePlayListSmallPicture",
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            routes.BuildSmallPicturePath(playList.SmallPictureName)));
                }

                if (playList.MediumPictureName != routes.PresetMediumPicture)
                {
                    routingSlipBuilder.AddActivity(
                        "RemovePlayListMediumPicture",
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            routes.BuildMediumPicturePath(playList.MediumPictureName)));
                }

                if (playList.LargePictureName != routes.PresetLargePicture)
                {
                    routingSlipBuilder.AddActivity(
                        "RemovePlayListLargePicture",
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            routes.BuildLargePicturePath(playList.LargePictureName)));
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
