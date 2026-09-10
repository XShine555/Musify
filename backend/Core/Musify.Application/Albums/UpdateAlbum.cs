using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Shared;
using Musify.Application.Events;
using Musify.Application.Albums.Responses;
using Musify.Application.Services;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Application.Contracts;

namespace Musify.Application.Albums
{
    public record UpdateAlbumCommand(
        long UserId,
        Guid AlbumId,
        string Title,
        string? Description,
        int? ReleaseYear,
        Guid? NewPictureIntentId)
        : ICommand<ErrorOr<AlbumApplicationResponse>>;

    public class UpdateAlbumCommandHandler(
        IEventBus eventBus,
        IDatabase database,
        UploadIntentValidator uploadIntentValidator,
        ILogger<UpdateAlbumCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        AlbumConfiguration albumConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : ICommandHandler<UpdateAlbumCommand, ErrorOr<AlbumApplicationResponse>>
    {
        public async ValueTask<ErrorOr<AlbumApplicationResponse>> Handle(UpdateAlbumCommand request, CancellationToken cancellationToken)
        {
            var album = await database.UserAlbums
                .SingleOrDefaultAsync(a => a.Id == request.AlbumId, cancellationToken);
            if (album is null)
            {
                logger.LogInformation("Album {AlbumId} not found", request.AlbumId);
                return Error.NotFound();
            }

            if (album.OwnerUserId != request.UserId)
            {
                logger.LogWarning("Album {AlbumId} does not belong to user {UserId}", request.AlbumId, request.UserId);
                return Error.Unauthorized();
            }

            var title = request.Title.Trim();

            album.Title = title;
            album.NormalizedTitle = title.ToUpperInvariant();
            album.Description = request.Description;
            album.ReleaseYear = request.ReleaseYear;

            UploadIntent? pictureIntent = null;
            string? finalPictureKey = null;

            if (request.NewPictureIntentId.HasValue)
            {
                var validation = await uploadIntentValidator.ValidateAndLoadAsync(
                    uploadIntentConfiguration,
                    request.NewPictureIntentId.Value, request.UserId, cancellationToken);
                if (validation.IsError)
                    return validation.Errors;

                pictureIntent = validation.Value;
                album.Pictures = new AlbumPictures
                {
                    OriginalName = pictureIntent.ObjectName,
                    SmallName = pictureIntent.ObjectName,
                    MediumName = pictureIntent.ObjectName,
                    LargeName = pictureIntent.ObjectName
                };
                finalPictureKey = albumConfiguration.Routes.BuildOriginalPicturePath(request.UserId, pictureIntent.ObjectName);
            }

            database.UserAlbums.Update(album);

            if (pictureIntent != null && finalPictureKey != null)
            {
                var publishResult = await PublishUpdateAlbumPictureSourceEventAsync(
                    album.Id, pictureIntent, finalPictureKey, cancellationToken);
                if (publishResult.IsError)
                    return publishResult.Errors;
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to update album {AlbumId}", request.AlbumId);
                return Error.Failure(description: $"Failed to update album {request.AlbumId}");
            }

            var albumTracks = database.AlbumHasTracks
                .AsNoTracking()
                .Where(albumTrack => albumTrack.AlbumId == album.Id);

            var trackCount = await albumTracks.CountAsync(cancellationToken);
            var coverTrackIds = await albumTracks
                .OrderBy(albumTrack => albumTrack.TrackNumber)
                .Take(AlbumApplicationResponse.CoverTrackCount)
                .Select(albumTrack => albumTrack.TrackId)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Updated album {AlbumId}", album.Id);
            return AlbumApplicationResponse.FromEntity(album, trackCount, coverTrackIds);
        }

        private async Task<ErrorOr<Success>> PublishUpdateAlbumPictureSourceEventAsync(
            Guid albumId,
            UploadIntent pictureIntent,
            string finalPictureKey,
            CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(
                    new UpdateAlbumPictureSourceEvent(
                        albumId,
                        pictureIntent.Id,
                        storageConfiguration.Bucket,
                        pictureIntent.Key,
                        finalPictureKey,
                        new ImageSize(
                            albumConfiguration.Routes.SmallPicturesPath,
                            albumConfiguration.PicturesSizes.SmallPictureWidth,
                            albumConfiguration.PicturesSizes.SmallPictureHeight),
                        new ImageSize(
                            albumConfiguration.Routes.MediumPicturesPath,
                            albumConfiguration.PicturesSizes.MediumPictureWidth,
                            albumConfiguration.PicturesSizes.MediumPictureHeight),
                        new ImageSize(
                            albumConfiguration.Routes.LargePicturesPath,
                            albumConfiguration.PicturesSizes.LargePictureWidth,
                            albumConfiguration.PicturesSizes.LargePictureHeight)),
                    cancellationToken);
                return new Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish update album picture source event for album {AlbumId}", albumId);
                return Error.Failure(description: $"Failed to publish update album picture source event for album {albumId}");
            }
        }
    }
}
