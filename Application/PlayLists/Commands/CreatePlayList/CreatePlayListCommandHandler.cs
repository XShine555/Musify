using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Application;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Pictures.Contracts;
using Musify.Application.PlayLists.Contracts;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Commands.CreatePlayList
{
    public class CreatePlayListCommandHandler(IPictureService pictureService, IDatabase database, ILogger<CreatePlayListCommandHandler> logger,
        PlayListConfiguration playListConfiguration)
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
                SmallPictureKeyName = playListConfiguration.Routes.SmallPictures,
                MediumPictureKeyName = playListConfiguration.Routes.MediumPictures,
                LargePictureKeyName = playListConfiguration.Routes.LargePictures,
            };

            await database.PlayLists.AddAsync(playList, cancellationToken);

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("PlayList with Id={PlayListId} created successfully for UserId={UserId}.", playList.Id, request.UserId);

            await pictureService.ResizePictureAsync(
                EntityType.PlayList,
                playList.Id,
                playList.Id.ToString(),
                request.PictureContentType,
                request.PictureStream,
                [ 
                    new PictureResize(
                        playListConfiguration.PicturesSizes.SmallPictureWidth,
                        playListConfiguration.PicturesSizes.SmallPictureHeight,
                        playListConfiguration.Routes.SmallPictures),
                    new PictureResize(
                        playListConfiguration.PicturesSizes.MediumPictureWidth,
                        playListConfiguration.PicturesSizes.MediumPictureHeight,
                        playListConfiguration.Routes.MediumPictures),
                    new PictureResize(
                        playListConfiguration.PicturesSizes.LargePictureWidth,
                        playListConfiguration.PicturesSizes.LargePictureHeight,
                        playListConfiguration.Routes.LargePictures)
                ],
                cancellationToken);

            logger.LogInformation("Picture for PlayList with Id={PlayListId} ", playList.Id);

            return Result.Created(PlayListResponse.FromEntity(playList));
        }
    }
}