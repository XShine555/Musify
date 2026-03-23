using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Application;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Handlers
{
    public class CreatePlayListCommandHandler(IEventBus eventBus, IDatabase database, IStorageHandler storageHandler, IPictureHandler pictureHandler,
        ILogger<CreatePlayListCommandHandler> logger, ApplicationStorageConfiguration storageConfiguration, PlayListConfiguration playListConfiguration)
        : IRequestHandler<CreatePlayListCommand, Task<Result<PlayListResponse>> >
    {
        public async Task<Result<PlayListResponse>> Handle(CreatePlayListCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);

            if (!userExists)
            {
                logger.LogWarning("User with Id={UserId} does not exist.", request.UserId);
                return Result.NotFound($"User with Id {request.UserId} does not exist.");
            }

            var playList = new PlayList
            {
                UserId = request.UserId,
                Name = request.Name,
                NormalizedName = request.Name.Trim().ToUpperInvariant(),
                Description = request.Description,
                SmallPictureKeyName = playListConfiguration.Routes.PresetSmallPicture,
                MediumPictureKeyName = playListConfiguration.Routes.PresetMediumPicture,
                LargePictureKeyName = playListConfiguration.Routes.PresetLargePicture,
            };

            var setPictureResult = request.Picture is null
                ? Result.Success()
                : await UploadPicture(playList, request.Picture, cancellationToken);

            if (!setPictureResult.IsSuccess)
                return setPictureResult;

            await database.PlayLists.AddAsync(playList, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("PlayList with id={PlayListId} created successfully for user with id={UserId}.", playList.Id, request.UserId);
            return Result.Created(PlayListResponse.FromEntity(playList));
        }

        async Task<Result> UploadPicture(PlayList playList, IFileData picture, CancellationToken cancellationToken)
        {
            var imageId = Guid.NewGuid();
            var pictureName = imageId + picture.FileType;
            var pictureStorageKey = Path.Join(playListConfiguration.Routes.OriginalPictures, pictureName);

            var uploadResult = await UploadFile(picture, pictureStorageKey, cancellationToken);

            if (!uploadResult.IsSuccess)
            {
                logger.LogError("Failed to upload picture for PlayList with Id={PlayListId} to storage. Storage handler returned error: {ErrorMessage}",
                    playList.Id, string.Join("; ", uploadResult.Errors));
                return Result.Error($"Failed to upload picture for PlayList with Id {playList.Id} to storage.");
            }
            logger.LogInformation("Playlist image uploaded successfully for PlayList with Id={PlayListId}.", playList.Id);

            try
            {
                await PublishUpdateEvent(pictureStorageKey, playList, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish resize picture event for PlayList with id={PlayListId}.", playList.Id);
                return Result.Error($"Failed to publish resize picture event for PlayList with id {playList.Id}.");
            }
            return Result.Success();
        }

        async Task<Result<string>> UploadFile(IFileData fileData, string storageKeyName, CancellationToken cancellationToken)
        {
            var uploadFileResult = await storageHandler.UploadFileAsync(
                fileData.FileStream,
                fileData.ContentType,
                storageConfiguration.BucketName,
                storageKeyName,
                cancellationToken);

            return uploadFileResult;
        }

        async Task PublishUpdateEvent(string originalPictureKeyName, PlayList playList, CancellationToken cancellationToken)
        {
            var smallPictureKeyName = Path.Combine(
                playListConfiguration.Routes.SmallPictures,
                $"{playList.Id}.{pictureHandler.FileExtension}");
            var mediumPictureKeyName = Path.Combine(
                playListConfiguration.Routes.MediumPictures,
                $"{playList.Id}.{pictureHandler.FileExtension}");
            var largePictureKeyName = Path.Combine(
                playListConfiguration.Routes.LargePictures,
                $"{playList.Id}.{pictureHandler.FileExtension}");

            await eventBus.PublishAsync(new UpdatePlayListPictureEvent(
                playList.Id,
                storageConfiguration.BucketName,
                originalPictureKeyName,
                smallPictureKeyName,
                playListConfiguration.PicturesSizes.SmallPictureWidth,
                playListConfiguration.PicturesSizes.SmallPictureHeight,
                mediumPictureKeyName,
                playListConfiguration.PicturesSizes.MediumPictureWidth,
                playListConfiguration.PicturesSizes.MediumPictureHeight,
                largePictureKeyName,
                playListConfiguration.PicturesSizes.LargePictureWidth,
                playListConfiguration.PicturesSizes.LargePictureHeight), cancellationToken);
        }
    }
}