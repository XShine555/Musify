using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Pictures;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Services;
using Musify.Application.Shared;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists
{
    public record CreatePlayListCommand(
        long UserId,
        string Name,
        string? Description,
        Guid? PictureIntentId,
        PlayListVisibility Visibility = PlayListVisibility.Private)
        : ICommand<ErrorOr<PlayListApplicationResponse>>;

    public class CreatePlayListCommandHandler(
        IEventBus eventBus,
        IDatabase database,
        UploadIntentValidator uploadIntentValidator,
        ILogger<CreatePlayListCommandHandler> logger,
        PlayListConfiguration playListConfiguration)
        : ICommandHandler<CreatePlayListCommand, ErrorOr<PlayListApplicationResponse>>
    {
        public async ValueTask<ErrorOr<PlayListApplicationResponse>> Handle(CreatePlayListCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
                return AppErrors.NotFound("User", request.UserId);

            PreparedPicture? picture = null;
            if (request.PictureIntentId is { } intentId)
            {
                var prepared = await PictureSourceChange.PrepareAsync(
                    uploadIntentValidator, intentId, request.UserId, UploadIntentPurpose.PlayListPicture, playListConfiguration, cancellationToken);
                if (prepared.IsError)
                    return prepared.Errors;

                picture = prepared.Value;
            }

            var name = request.Name.Trim();
            var playList = new PlayList
            {
                OwnerUserId = request.UserId,
                Name = name,
                NormalizedName = TextNormalizer.Normalize(name),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Pictures = picture?.Pictures,
                Visibility = request.Visibility
            };
            await database.PlayLists.AddAsync(playList, cancellationToken);

            if (picture != null)
            {
                await eventBus.PublishAsync(
                    new CreatePlayListResourcesEvent(
                        playList.Id, picture.Intent.Id, picture.Intent.Bucket, picture.Intent.Key, picture.FinalKey, picture.Sizes),
                    cancellationToken);
            }

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created playlist {PlayListId} for user {UserId}", playList.Id, request.UserId);

            return PlayListApplicationResponse.FromEntity(playList);
        }
    }
}
