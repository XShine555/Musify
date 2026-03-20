using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Application.PlayLists.Contracts;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Commands.CreatePlayList
{
    public class CreatePlayListCommandHandler(IEventBus eventBus, IDatabase database, IStorageHandler storageHandler,
        ILogger<CreatePlayListCommandHandler> logger, ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration, IPictureHandler pictureHandler)
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

            if (request.Picture is not null)
            {
                var imageId = Guid.NewGuid();
                var pictureName = $"{imageId}{request.Picture.FileType}";
                var originalImageStorageKey = Path.Join(playListConfiguration.Routes.OriginalPictures, pictureName);

                var uploadResult = await storageHandler.UploadFileAsync(
                    request.Picture.FileStream,
                    request.Picture.ContentType,
                    storageConfiguration.BucketName,
                    originalImageStorageKey,
                    cancellationToken);

                if (!uploadResult.IsSuccess)
                {
                    logger.LogError("Failed to upload picture for PlayList with Id={PlayListId} to storage. Storage handler returned error: {ErrorMessage}",
                        playList.Id, string.Join("; ", uploadResult.Errors));
                    return Result.Error($"Failed to upload picture for PlayList with Id {playList.Id} to storage.");
                }

                playList.OriginalPictureKeyName = pictureName;
                logger.LogInformation("Playlist image uploaded successfully for PlayList with Id={PlayListId}.", playList.Id);

                var smallPictureKeyName = Path.Combine(
                    playListConfiguration.Routes.SmallPictures,
                    $"{playList.Id}.{pictureHandler.FileExtension}");
                var mediumPictureKeyName = Path.Combine(
                    playListConfiguration.Routes.MediumPictures,
                    $"{playList.Id}.{pictureHandler.FileExtension}");
                var largePictureKeyName = Path.Combine(
                    playListConfiguration.Routes.LargePictures,
                    $"{playList.Id}.{pictureHandler.FileExtension}");

                try
                {
                    await eventBus.PublishAsync(new UpdatePlayListPictureEvent(
                        playList.Id,
                        storageConfiguration.BucketName,
                        originalImageStorageKey,
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
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to publish resize picture event for PlayList with id={PlayListId}.", playList.Id);
                    return Result.Error($"Failed to publish resize picture event for PlayList with id {playList.Id}.");
                }
            }
            else
            {
                logger.LogInformation("No picture provided for PlayList with Id={PlayListId}. Using Preset pictures.", playList.Id);
            }

            await database.PlayLists.AddAsync(playList, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("PlayList with id={PlayListId} created successfully for user with id={UserId}.", playList.Id, request.UserId);
            return Result.Created(PlayListResponse.FromEntity(playList));
        }
    }
}