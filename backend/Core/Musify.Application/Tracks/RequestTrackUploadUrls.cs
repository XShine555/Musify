using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks;

public record RequestTrackUploadUrlsCommand(
    long UserId,
    string PictureFileType,
    string PictureContentType,
    string AudioFileType,
    string AudioContentType,
    long? ExpectedPictureSizeBytes = null,
    long? ExpectedAudioSizeBytes = null)
    : ICommand<ErrorOr<TrackUploadUrlsResponse>>;

public class RequestTrackUploadUrlsCommandHandler(
    IDatabase database,
    UploadIntentValidator uploadIntentValidator,
    IStorageService storageService,
    ILogger<RequestTrackUploadUrlsCommandHandler> logger,
    ApplicationStorageConfiguration storageConfiguration,
    TrackConfiguration trackConfiguration,
    UploadIntentConfiguration uploadIntentConfiguration)
    : ICommandHandler<RequestTrackUploadUrlsCommand, ErrorOr<TrackUploadUrlsResponse>>
{
    public async ValueTask<ErrorOr<TrackUploadUrlsResponse>> Handle(RequestTrackUploadUrlsCommand request, CancellationToken cancellationToken)
    {
        var userExists = await database.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            logger.LogWarning("User {UserId} not found", request.UserId);
            return Error.NotFound(description: $"User {request.UserId} not found");
        }

        var effectivePictureSize = request.ExpectedPictureSizeBytes
            ?? uploadIntentConfiguration.DefaultExpectedPictureSizeBytes;
        var effectiveAudioSize = request.ExpectedAudioSizeBytes
            ?? uploadIntentConfiguration.DefaultExpectedAudioSizeBytes;

        var pictureObjectName = $"{Guid.NewGuid()}.{request.PictureFileType.TrimStart('.').ToLowerInvariant()}";
        var audioObjectName = $"{Guid.NewGuid()}.{request.AudioFileType.TrimStart('.').ToLowerInvariant()}";

        var tempPictureKey = trackConfiguration.Routes.BuildTempPicturePath(
            uploadIntentConfiguration.TempRootPrefix, request.UserId, pictureObjectName);
        var tempAudioKey = trackConfiguration.Routes.BuildTempAudioPath(
            uploadIntentConfiguration.TempRootPrefix, request.UserId, audioObjectName);

        try
        {
            var expiresIn = TimeSpan.FromSeconds(uploadIntentConfiguration.UploadUrlExpiresInSeconds);

            var pictureUploadUrl = await storageService.GetUploadUrlAsync(
                storageConfiguration.Bucket,
                tempPictureKey,
                request.PictureContentType,
                expiresIn,
                cancellationToken);

            var audioUploadUrl = await storageService.GetUploadUrlAsync(
                storageConfiguration.Bucket,
                tempAudioKey,
                request.AudioContentType,
                expiresIn,
                cancellationToken);

            var expiresAt = DateTime.UtcNow.AddSeconds(uploadIntentConfiguration.UploadUrlExpiresInSeconds);

            await using var transaction = await database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);

            var quotaCheck = await uploadIntentValidator.CheckQuotaAsync(
                uploadIntentConfiguration,
                request.UserId, effectivePictureSize + effectiveAudioSize, 2, cancellationToken);
            if (quotaCheck.IsError)
            {
                logger.LogWarning("User {UserId} failed upload intent quota check", request.UserId);
                return quotaCheck.Errors;
            }

            var pictureIntent = new UploadIntent
            {
                UserId = request.UserId,
                Bucket = storageConfiguration.Bucket,
                Key = tempPictureKey,
                ObjectName = pictureObjectName,
                ContentType = request.PictureContentType,
                ExpectedSizeBytes = effectivePictureSize,
                Purpose = UploadIntentPurpose.TrackPicture,
                ExpiresAt = expiresAt,
            };

            var audioIntent = new UploadIntent
            {
                UserId = request.UserId,
                Bucket = storageConfiguration.Bucket,
                Key = tempAudioKey,
                ObjectName = audioObjectName,
                ContentType = request.AudioContentType,
                ExpectedSizeBytes = effectiveAudioSize,
                Purpose = UploadIntentPurpose.TrackAudio,
                ExpiresAt = expiresAt,
            };

            await database.UploadIntents.AddAsync(pictureIntent, cancellationToken);
            await database.UploadIntents.AddAsync(audioIntent, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Issued upload intents {PictureIntentId}/{AudioIntentId} for user {UserId} (TrackPicture/TrackAudio)",
                pictureIntent.Id, audioIntent.Id, request.UserId);

            return new TrackUploadUrlsResponse(
                pictureIntent.Id,
                audioIntent.Id,
                storageConfiguration.Bucket,
                tempPictureKey,
                pictureObjectName,
                request.PictureContentType,
                pictureUploadUrl,
                tempAudioKey,
                audioObjectName,
                request.AudioContentType,
                audioUploadUrl,
                uploadIntentConfiguration.UploadUrlExpiresInSeconds);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to generate track upload URLs for user {UserId}", request.UserId);
            return Error.Failure(description: "Failed to generate upload URLs");
        }
    }
}
