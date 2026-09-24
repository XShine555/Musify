using MassTransit;
using MassTransit.Courier.Contracts;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.Albums;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

public class DeleteAlbumRoutingSlipBuilder(
    IDatabase database,
    AlbumConfiguration albumConfiguration,
    ApplicationStorageConfiguration storageConfiguration)
{
    public async Task<RoutingSlipBuilder> BuildAsync(
        Guid albumId,
        long userId,
        Guid? correlationId,
        CancellationToken cancellationToken)
    {
        var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());
        routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

        routingSlipBuilder.AddSubscription(
            EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
            RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

        var album = await database.Albums
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == albumId, cancellationToken);

        routingSlipBuilder.AddActivity(
            ActivityNames.MarkAlbumAsRemoving,
            EndpointHelper.BuildExecuteActivityUri(MarkAlbumAsRemovingActivity.ExecuteEndpointName),
            new MarkAlbumAsRemovingArguments(albumId));

        if (album?.Pictures != null)
        {
            var bucket = storageConfiguration.Bucket;
            var routes = albumConfiguration.Routes;

            if (album.Pictures.OriginalName is { } originalName)
            {
                routingSlipBuilder.AddActivity(
                    ActivityNames.RemoveAlbumOriginalPicture,
                    EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                    new RemoveFileFromBucketArguments(
                        bucket,
                        routes.BuildOriginalPicturePath(userId, originalName)));
            }

            if (album.Pictures.SmallName is { } smallName)
            {
                routingSlipBuilder.AddActivity(
                    ActivityNames.RemoveAlbumSmallPicture,
                    EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                    new RemoveFileFromBucketArguments(
                        bucket,
                        routes.BuildSmallPicturePath(smallName)));
            }

            if (album.Pictures.MediumName is { } mediumName)
            {
                routingSlipBuilder.AddActivity(
                    ActivityNames.RemoveAlbumMediumPicture,
                    EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                    new RemoveFileFromBucketArguments(
                        bucket,
                        routes.BuildMediumPicturePath(mediumName)));
            }

            if (album.Pictures.LargeName is { } largeName)
            {
                routingSlipBuilder.AddActivity(
                    ActivityNames.RemoveAlbumLargePicture,
                    EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                    new RemoveFileFromBucketArguments(
                        bucket,
                        routes.BuildLargePicturePath(largeName)));
            }
        }

        routingSlipBuilder.AddActivity(
            ActivityNames.DeleteAlbumFromDb,
            EndpointHelper.BuildExecuteActivityUri(DeleteAlbumFromDbActivity.ExecuteEndpointName),
            new DeleteAlbumFromDbArguments(albumId));

        return routingSlipBuilder;
    }
}
