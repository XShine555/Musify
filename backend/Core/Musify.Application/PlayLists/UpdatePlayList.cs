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
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists;

public record UpdatePlayListCommand(
    long UserId,
    Guid PlayListId,
    string? NewName,
    string? NewDescription,
    Guid? NewPictureIntentId,
    PlayListVisibility? NewVisibility = null)
    : ICommand<ErrorOr<PlayListApplicationResponse>>;

public class UpdatePlayListCommandHandler(
    IEventBus eventBus,
    IDatabase database,
    UploadIntentValidator uploadIntentValidator,
    ILogger<UpdatePlayListCommandHandler> logger,
    PlayListConfiguration playListConfiguration)
    : ICommandHandler<UpdatePlayListCommand, ErrorOr<PlayListApplicationResponse>>
{
    public async ValueTask<ErrorOr<PlayListApplicationResponse>> Handle(UpdatePlayListCommand request, CancellationToken cancellationToken)
    {
        var found = await database.PlayLists.FindOwnedAsync(request.PlayListId, request.UserId, cancellationToken);
        if (found.IsError)
            return found.Errors;

        var playList = found.Value;

        if (!string.IsNullOrWhiteSpace(request.NewName))
        {
            playList.Name = request.NewName.Trim();
            playList.NormalizedName = TextNormalizer.Normalize(request.NewName);
        }

        // null means "unchanged"; a blank description clears it.
        if (request.NewDescription != null)
            playList.Description = string.IsNullOrWhiteSpace(request.NewDescription) ? null : request.NewDescription.Trim();

        if (request.NewVisibility is { } visibility)
            playList.Visibility = visibility;

        if (request.NewPictureIntentId is { } intentId)
        {
            var prepared = await PictureSourceChange.PrepareAsync(
                uploadIntentValidator, intentId, request.UserId, UploadIntentPurpose.PlayListPicture, playListConfiguration, cancellationToken);
            if (prepared.IsError)
                return prepared.Errors;

            var picture = prepared.Value;
            playList.Pictures = picture.Pictures;

            await eventBus.PublishAsync(
                new UpdatePlayListPictureSourceEvent(
                    playList.Id, picture.Intent.Id, picture.Intent.Bucket, picture.Intent.Key, picture.FinalKey, picture.Sizes),
                cancellationToken);
        }

        await database.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated playlist {PlayListId}", playList.Id);

        return await database.PlayLists
            .AsNoTracking()
            .Where(p => p.Id == playList.Id)
            .SelectResponse()
            .SingleAsync(cancellationToken);
    }
}
