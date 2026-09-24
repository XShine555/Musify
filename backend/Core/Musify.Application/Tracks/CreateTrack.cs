using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Events;
using Musify.Application.Services;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks;

public record CreateTrackCommand(
    long UserId,
    string Title,
    Guid PictureIntentId,
    Guid AudioIntentId,
    IReadOnlyCollection<Genre> Tags,
    bool IsExplicit = false)
    : ICommand<ErrorOr<TrackApplicationResponse>>;

public class CreateTrackCommandHandler(
    IDatabase database,
    IEventBus eventBus,
    UploadIntentValidator uploadIntentValidator,
    ILogger<CreateTrackCommandHandler> logger,
    TrackConfiguration trackConfiguration)
    : ICommandHandler<CreateTrackCommand, ErrorOr<TrackApplicationResponse>>
{
    public async ValueTask<ErrorOr<TrackApplicationResponse>> Handle(CreateTrackCommand request, CancellationToken cancellationToken)
    {
        var user = await database.Users
            .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User {UserId} not found", request.UserId);
            return Error.NotFound(description: $"User {request.UserId} not found");
        }

        var distinctTags = request.Tags.Distinct().ToList();
        if (distinctTags.Count == 0)
        {
            logger.LogWarning("Track {Title} was submitted without any tags", request.Title);
            return Error.Validation(description: "At least one tag is required.");
        }

        var tagConflicts = GenreCompatibility.FindConflicts(distinctTags);
        if (tagConflicts.Count > 0)
        {
            var conflict = tagConflicts.First();
            logger.LogWarning(
                "Track {Title} was submitted with incompatible tags {First} and {Second}",
                request.Title, conflict.First, conflict.Second);
            return Error.Validation(description: $"Tags '{conflict.First}' and '{conflict.Second}' are not compatible.");
        }

        if (request.PictureIntentId == request.AudioIntentId)
            return Error.Validation(description: "Picture and audio must use different upload intents.");

        var pictureValidation = await uploadIntentValidator.ValidateAndLoadAsync(
            request.PictureIntentId, request.UserId, UploadIntentPurpose.TrackPicture, cancellationToken);
        if (pictureValidation.IsError)
            return pictureValidation.Errors;

        var audioValidation = await uploadIntentValidator.ValidateAndLoadAsync(
            request.AudioIntentId, request.UserId, UploadIntentPurpose.TrackAudio, cancellationToken);
        if (audioValidation.IsError)
            return audioValidation.Errors;

        var pictureIntent = pictureValidation.Value;
        var audioIntent = audioValidation.Value;

        var finalPictureKey = trackConfiguration.Routes.BuildOriginalPicturePath(request.UserId, pictureIntent.ObjectName);
        var finalAudioKey = trackConfiguration.Routes.BuildOriginalAudioPath(request.UserId, audioIntent.ObjectName);
        var audioProcessedFolderKey = trackConfiguration.Routes.BuildProcessedAudioPath(Guid.NewGuid().ToString());

        var trackEntity = new Track
        {
            Title = request.Title.Trim(),
            NormalizedTitle = TextNormalizer.Normalize(request.Title),
            OwnerUserId = user.Id,
            Owner = user,
            IsExplicit = request.IsExplicit,
            Pictures = new TrackPictures
            {
                OriginalName = pictureIntent.ObjectName,
                SmallName = trackConfiguration.Routes.PresetSmallPicture,
                MediumName = trackConfiguration.Routes.PresetMediumPicture,
                LargeName = trackConfiguration.Routes.PresetLargePicture,
                ProcessingStatus = ProcessingStatus.Pending
            },
            Audio = new TrackAudio
            {
                OriginalName = audioIntent.ObjectName,
                TranscodeStatus = ProcessingStatus.Pending
            }
        };

        trackEntity.Tags = distinctTags
            .Select(tag => new TrackTag { TrackId = trackEntity.Id, Tag = tag })
            .ToList();

        await database.Tracks.AddAsync(trackEntity, cancellationToken);

        await database.UserHasTracks.AddAsync(new UserHasTrack
        {
            UserId = request.UserId,
            TrackId = trackEntity.Id
        }, cancellationToken);

        var sizes = trackConfiguration.PicturesSizes.ToImageSizes(trackConfiguration.Routes);
        await eventBus.PublishAsync(
            new CreateTrackResourcesEvent(
                trackEntity.Id,
                pictureIntent.Id,
                audioIntent.Id,
                pictureIntent.Bucket,
                pictureIntent.Key,
                finalPictureKey,
                audioIntent.Key,
                finalAudioKey,
                audioProcessedFolderKey,
                sizes),
            cancellationToken);

        await database.SaveChangesAsync(cancellationToken);

        return TrackApplicationResponse.FromEntity(trackEntity, listensCount: 0);
    }
}
