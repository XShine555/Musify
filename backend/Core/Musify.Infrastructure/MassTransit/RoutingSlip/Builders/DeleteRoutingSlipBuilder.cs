using MassTransit;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Activities.LifeCycle;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    /// <summary>Marks an entity as removing, deletes its files from the bucket and finally deletes its row.</summary>
    public class DeleteRoutingSlipBuilder(
        IDatabase database,
        TrackConfiguration trackConfiguration,
        AlbumConfiguration albumConfiguration,
        PlayListConfiguration playListConfiguration,
        ApplicationStorageConfiguration storageConfiguration)
    {
        private sealed record PictureStepNames(string Original, string Small, string Medium, string Large);

        private static readonly PictureStepNames AlbumSteps = new(
            ActivityNames.RemoveAlbumOriginalPicture, ActivityNames.RemoveAlbumSmallPicture,
            ActivityNames.RemoveAlbumMediumPicture, ActivityNames.RemoveAlbumLargePicture);

        private static readonly PictureStepNames PlayListSteps = new(
            ActivityNames.RemovePlayListOriginalPicture, ActivityNames.RemovePlayListSmallPicture,
            ActivityNames.RemovePlayListMediumPicture, ActivityNames.RemovePlayListLargePicture);

        public async Task<RoutingSlipBuilder> BuildTrackAsync(
            Guid trackId, long userId, Guid? correlationId, CancellationToken cancellationToken)
        {
            var builder = RoutingSlips.Create(correlationId)
                .AddStep(
                    ActivityNames.MarkTrackAsRemoving,
                    MarkTrackLifeCycleActivity.ExecuteEndpointName,
                    new MarkLifeCycleArguments(trackId, LifeCycleStatus.Removing));

            var track = await database.Tracks.AsNoTracking().SingleOrDefaultAsync(t => t.Id == trackId, cancellationToken);
            if (track != null)
            {
                var bucket = storageConfiguration.Bucket;
                var routes = trackConfiguration.Routes;

                if (!string.IsNullOrEmpty(track.Pictures.OriginalName))
                {
                    builder.AddRemoveFile(
                        ActivityNames.RemoveTrackOriginalPicture, bucket,
                        routes.BuildOriginalPicturePath(userId, track.Pictures.OriginalName));
                }

                // Unprocessed tracks still point at the shared preset pictures, which must never be deleted.
                if (track.Pictures.IsProcessed)
                {
                    builder.AddRemoveFile(ActivityNames.RemoveTrackSmallPicture, bucket, routes.BuildPicturePath(PictureSize.Small, track.Pictures.SmallName));
                    builder.AddRemoveFile(ActivityNames.RemoveTrackMediumPicture, bucket, routes.BuildPicturePath(PictureSize.Medium, track.Pictures.MediumName));
                    builder.AddRemoveFile(ActivityNames.RemoveTrackLargePicture, bucket, routes.BuildPicturePath(PictureSize.Large, track.Pictures.LargeName));
                }

                if (!string.IsNullOrEmpty(track.Audio.OriginalName))
                {
                    builder.AddRemoveFile(
                        ActivityNames.RemoveTrackOriginalAudio, bucket,
                        routes.BuildOriginalAudioPath(userId, track.Audio.OriginalName));
                }

                if (track.Audio.IsProcessed)
                {
                    builder.AddRemoveFile(
                        ActivityNames.RemoveTrackProcessedAudio, bucket,
                        routes.BuildProcessedAudioPath(track.Audio.FolderName));
                }
            }

            return builder.AddStep(
                ActivityNames.DeleteTrackFromDb, DeleteTrackActivity.ExecuteEndpointName, new DeleteEntityArguments(trackId));
        }

        public async Task<RoutingSlipBuilder> BuildAlbumAsync(
            Guid albumId, long userId, Guid? correlationId, CancellationToken cancellationToken)
        {
            var builder = RoutingSlips.Create(correlationId)
                .AddStep(
                    ActivityNames.MarkAlbumAsRemoving,
                    MarkAlbumLifeCycleActivity.ExecuteEndpointName,
                    new MarkLifeCycleArguments(albumId, LifeCycleStatus.Removing));

            var pictures = await database.Albums.AsNoTracking()
                .Where(a => a.Id == albumId)
                .Select(a => a.Pictures)
                .SingleOrDefaultAsync(cancellationToken);

            AddPictureRemovals(builder, AlbumSteps, albumConfiguration.Routes, userId, pictures);

            return builder.AddStep(
                ActivityNames.DeleteAlbumFromDb, DeleteAlbumActivity.ExecuteEndpointName, new DeleteEntityArguments(albumId));
        }

        public async Task<RoutingSlipBuilder> BuildPlayListAsync(
            Guid playListId, long userId, Guid? correlationId, CancellationToken cancellationToken)
        {
            var builder = RoutingSlips.Create(correlationId)
                .AddStep(
                    ActivityNames.MarkPlayListAsRemoving,
                    MarkPlayListLifeCycleActivity.ExecuteEndpointName,
                    new MarkLifeCycleArguments(playListId, LifeCycleStatus.Removing));

            var pictures = await database.PlayLists.AsNoTracking()
                .Where(p => p.Id == playListId)
                .Select(p => p.Pictures)
                .SingleOrDefaultAsync(cancellationToken);

            AddPictureRemovals(builder, PlayListSteps, playListConfiguration.Routes, userId, pictures);

            return builder.AddStep(
                ActivityNames.DeletePlayListFromDb, DeletePlayListActivity.ExecuteEndpointName, new DeleteEntityArguments(playListId));
        }

        private void AddPictureRemovals(
            RoutingSlipBuilder builder, PictureStepNames steps, PictureRoutes routes, long userId, EntityPictures? pictures)
        {
            if (pictures == null)
                return;

            var bucket = storageConfiguration.Bucket;

            if (pictures.OriginalName is { } original)
                builder.AddRemoveFile(steps.Original, bucket, routes.BuildOriginalPicturePath(userId, original));
            if (pictures.SmallName is { } small)
                builder.AddRemoveFile(steps.Small, bucket, routes.BuildPicturePath(PictureSize.Small, small));
            if (pictures.MediumName is { } medium)
                builder.AddRemoveFile(steps.Medium, bucket, routes.BuildPicturePath(PictureSize.Medium, medium));
            if (pictures.LargeName is { } large)
                builder.AddRemoveFile(steps.Large, bucket, routes.BuildPicturePath(PictureSize.Large, large));
        }
    }
}
