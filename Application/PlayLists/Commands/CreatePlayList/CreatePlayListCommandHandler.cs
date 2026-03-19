using Ardalis.Result;
using DispatchR.Abstractions.Send;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Application.PlayLists.Contracts;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Commands.CreatePlayList
{
    public class CreatePlayListCommandHandler(IPublishEndpoint publishEndpoint, IDatabase database, IStorageHandler storageHandler,
        ILogger<CreatePlayListCommandHandler> logger, StorageSettings storageConfiguration, PlayListConfiguration playListConfiguration)
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

            var imageId = Guid.NewGuid();

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
                playList.OriginalPictureKeyName = $"{imageId}{request.Picture.FileType}";
                var imageKeyName = $"{playListConfiguration.Routes.OriginalPictures}/{imageId}{request.Picture.FileType}";

                var uploadResult = await storageHandler.UploadFileAsync(
                    request.Picture.FileStream,
                    request.Picture.ContentType,
                    storageConfiguration.BucketName,
                    imageKeyName,
                    cancellationToken);

                if (!uploadResult.IsSuccess)
                {
                    logger.LogError("Failed to upload picture for PlayList with Id={PlayListId} to storage. Storage handler returned error: {ErrorMessage}",
                        playList.Id, string.Join("; ", uploadResult.Errors));
                    return Result.Error($"Failed to upload picture for PlayList with Id {playList.Id} to storage.");
                }

                logger.LogInformation("Playlist image uploaded successfully for PlayList with Id={PlayListId}.", playList.Id);

                await publishEndpoint.Publish(new ResizePictureEvent(
                    storageConfiguration.BucketName,
                    imageKeyName,
                    [
                        new ResizePictureItems(
                        playListConfiguration.PicturesSizes.SmallPictureWidth,
                        playListConfiguration.PicturesSizes.SmallPictureHeight,
                        playListConfiguration.Routes.SmallPictures),
                    new ResizePictureItems(
                        playListConfiguration.PicturesSizes.MediumPictureWidth,
                        playListConfiguration.PicturesSizes.MediumPictureHeight,
                        playListConfiguration.Routes.MediumPictures),
                    new ResizePictureItems(
                        playListConfiguration.PicturesSizes.LargePictureWidth,
                        playListConfiguration.PicturesSizes.LargePictureHeight,
                        playListConfiguration.Routes.LargePictures)
                    ] ), cancellationToken);
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