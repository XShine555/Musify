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

            if (playList?.Pictures is not null)
            {
                var bucket = storageConfiguration.Bucket;
                var routes = playListConfiguration.Routes;

                if (playList.Pictures.OriginalName is { } originalName)
                {
                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemovePlayListOriginalPicture,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            routes.BuildOriginalPicturePath(userId, originalName)));
                }

                if (playList.Pictures.SmallName is { } smallName)
                {
                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemovePlayListSmallPicture,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            routes.BuildSmallPicturePath(smallName)));
                }

                if (playList.Pictures.MediumName is { } mediumName)
                {
                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemovePlayListMediumPicture,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            routes.BuildMediumPicturePath(mediumName)));
                }

                if (playList.Pictures.LargeName is { } largeName)
                {
                    routingSlipBuilder.AddActivity(
                        ActivityNames.RemovePlayListLargePicture,
                        EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                        new RemoveFileFromBucketArguments(
                            bucket,
                            routes.BuildLargePicturePath(largeName)));
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
