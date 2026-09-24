using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Events;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Services;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists;

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
    ApplicationStorageConfiguration storageConfiguration,
    PlayListConfiguration playListConfiguration,
    UploadIntentConfiguration uploadIntentConfiguration)
    : ICommandHandler<CreatePlayListCommand, ErrorOr<PlayListApplicationResponse>>
{
    private sealed record ResolvedPlayListPictures(EntityPictures? Pictures, UploadIntent? Intent);

    public async ValueTask<ErrorOr<PlayListApplicationResponse>> Handle(CreatePlayListCommand request, CancellationToken cancellationToken)
    {
        var userExists = await database.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            logger.LogWarning("User {UserId} not found", request.UserId);
            return Error.NotFound(description: $"User {request.UserId} not found");
        }

        var picturesResult = await ResolvePicturesAsync(request, cancellationToken);
        if (picturesResult.IsError)
            return picturesResult.Errors;

        var resolvedPictures = picturesResult.Value;

        var playList = new PlayList
        {
            OwnerUserId = request.UserId,
            Name = request.Name.Trim(),
            NormalizedName = TextNormalizer.Normalize(request.Name),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Pictures = resolvedPictures.Pictures,
            Visibility = request.Visibility
        };
        await database.PlayLists.AddAsync(playList, cancellationToken);

        if (resolvedPictures.Intent != null)
        {
            var finalPictureKey = playListConfiguration.Routes.BuildOriginalPicturePath(request.UserId, resolvedPictures.Pictures!.OriginalName);
            var publishResult = await PublishCreatePlayListEventAsync(playList.Id, resolvedPictures.Intent, finalPictureKey, cancellationToken);
            if (publishResult.IsError)
                return publishResult.Errors;
        }

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to save playlist for user {UserId}", request.UserId);
            return Error.Failure(description: $"Failed to create playlist for user {request.UserId}");
        }

        logger.LogInformation("Created playlist {PlayListId} for user {UserId}", playList.Id, request.UserId);
        return PlayListApplicationResponse.FromEntity(playList);
    }

    private async ValueTask<ErrorOr<ResolvedPlayListPictures>> ResolvePicturesAsync(
        CreatePlayListCommand request, CancellationToken cancellationToken)
    {
        if (!request.PictureIntentId.HasValue)
        {
            return new ResolvedPlayListPictures(null, null);
        }

        var validation = await uploadIntentValidator.ValidateAndLoadAsync(
            uploadIntentConfiguration, request.PictureIntentId.Value, request.UserId, UploadIntentPurpose.PlayListPicture, cancellationToken);

        if (validation.IsError)
            return validation.Errors;

        return new ResolvedPlayListPictures(EntityPictures.Pending(validation.Value.ObjectName), validation.Value);
    }

    private async Task<ErrorOr<Success>> PublishCreatePlayListEventAsync(
        Guid playListId,
        UploadIntent pictureIntent,
        string finalPictureKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var sizes = playListConfiguration.PicturesSizes.ToImageSizes(playListConfiguration.Routes);
            await eventBus.PublishAsync(
                new CreatePlayListResourcesEvent(
                    playListId,
                    pictureIntent.Id,
                    storageConfiguration.Bucket,
                    pictureIntent.Key,
                    finalPictureKey,
                    sizes.Small,
                    sizes.Medium,
                    sizes.Large),
                cancellationToken);
            return Result.Success;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish create playlist event for playlist {PlayListId}", playListId);
            return Error.Failure(description: $"Failed to publish create playlist event for playlist {playListId}");
        }
    }
}
