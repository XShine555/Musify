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
        ILogger<CreatePlayListCommandHandler> logger, StorageConfiguration storageConfiguration, PlayListConfiguration playListConfiguration)
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
                OriginalPictureKeyName = $"{imageId}{request.PictureFileType}",
                SmallPictureKeyName = playListConfiguration.Routes.PresetSmallPicture,
                MediumPictureKeyName = playListConfiguration.Routes.PresetMediumPicture,
                LargePictureKeyName = playListConfiguration.Routes.PresetLargePicture,
            };
            await database.PlayLists.AddAsync(playList, cancellationToken);

            var imageKeyName = $"{playListConfiguration.Routes.OriginalPictures}/{imageId}{request.PictureFileType}";

            var uploadResult = await storageHandler.UploadFileAsync(
                request.PictureStream,
                request.PictureContentType,
                storageConfiguration.BucketName,
                imageKeyName,
                cancellationToken);

            if (!uploadResult.IsSuccess)
            {
                logger.LogError("Failed to upload picture for PlayList with Id={PlayListId} to storage. Storage handler returned error: {ErrorMessage}",
                    playList.Id, string.Join("; ", uploadResult.Errors));
                return Result.Error($"Failed to upload picture for PlayList with Id {playList.Id} to storage.");
            }

            logger.LogInformation("PlayList with Id={PlayListId} created successfully for UserId={UserId}.", playList.Id, request.UserId);

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

            logger.LogInformation("Picture for PlayList with Id={PlayListId} ", playList.Id);

            return Result.Created(PlayListResponse.FromEntity(playList));
        }
    }
}