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

namespace Musify.Application.PlayLists
{
    public record UpdatePlayListPictureCommand(long UserId, Guid PlayListId, Guid PictureIntentId)
        : ICommand<ErrorOr<PlayListApplicationResponse>>;

    public class UpdatePlayListPictureCommandHandler(
        IEventBus eventBus,
        IDatabase database,
        UploadIntentValidator uploadIntentValidator,
        ILogger<UpdatePlayListPictureCommandHandler> logger,
        PlayListConfiguration playListConfiguration)
        : ICommandHandler<UpdatePlayListPictureCommand, ErrorOr<PlayListApplicationResponse>>
    {
        public async ValueTask<ErrorOr<PlayListApplicationResponse>> Handle(UpdatePlayListPictureCommand request, CancellationToken cancellationToken)
        {
            var found = await database.PlayLists.FindOwnedAsync(request.PlayListId, request.UserId, cancellationToken);
            if (found.IsError)
                return found.Errors;

            var playList = found.Value;

            var prepared = await PictureSourceChange.PrepareAsync(
                uploadIntentValidator, request.PictureIntentId, request.UserId, UploadIntentPurpose.PlayListPicture, playListConfiguration, cancellationToken);
            if (prepared.IsError)
                return prepared.Errors;

            var picture = prepared.Value;
            playList.Pictures = picture.Pictures;

            await eventBus.PublishAsync(
                new UpdatePlayListPictureSourceEvent(
                    playList.Id, picture.Intent.Id, picture.Intent.Bucket, picture.Intent.Key, picture.FinalKey, picture.Sizes),
                cancellationToken);

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated picture of playlist {PlayListId}", playList.Id);

            return await database.PlayLists
                .AsNoTracking()
                .Where(p => p.Id == playList.Id)
                .SelectResponse()
                .SingleAsync(cancellationToken);
        }
    }
}
