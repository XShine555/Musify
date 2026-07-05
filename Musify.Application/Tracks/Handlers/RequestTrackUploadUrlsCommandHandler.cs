using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Services;
using Musify.Application.Tracks.Commands;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Application.Contracts;

namespace Musify.Application.Tracks.Handlers
{
    public class RequestTrackUploadUrlsCommandHandler(
        IDatabase database,
        UploadIntentValidator uploadIntentValidator,
        IStorageService storageService,
        ILogger<RequestTrackUploadUrlsCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        TrackConfiguration trackConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : ICommandHandler<RequestTrackUploadUrlsCommand, Result<TrackUploadUrlsResponse>>
    {
        public async ValueTask<Result<TrackUploadUrlsResponse>> Handle(RequestTrackUploadUrlsCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Result.NotFound($"User {request.UserId} not found");
            }

            var effectivePictureSize = request.ExpectedPictureSizeBytes
                ?? uploadIntentConfiguration.DefaultExpectedPictureSizeBytes;
            var effectiveAudioSize = request.ExpectedAudioSizeBytes
                ?? uploadIntentConfiguration.DefaultExpectedAudioSizeBytes;

            var pictureObjectName = $"{Guid.NewGuid()}.{request.PictureFileType.TrimStart('.').ToLowerInvariant()}";
            var audioObjectName = $"{Guid.NewGuid()}.{request.AudioFileType.TrimStart('.').ToLowerInvariant()}";

            var tempPictureKey = trackConfiguration.Routes.BuildTempPicturePath(
                uploadIntentConfiguration.TemporalRootPrefix, request.UserId, pictureObjectName);
            var tempAudioKey = trackConfiguration.Routes.BuildTempAudioPath(
                uploadIntentConfiguration.TemporalRootPrefix, request.UserId, audioObjectName);

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
                if (!quotaCheck.IsSuccess)
                {
                    logger.LogWarning("User {UserId} failed upload intent quota check", request.UserId);
                    return quotaCheck;
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

                return Result.Success(new TrackUploadUrlsResponse(
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
                    uploadIntentConfiguration.UploadUrlExpiresInSeconds));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to generate track upload URLs for user {UserId}", request.UserId);
                return Result.Error("Failed to generate upload URLs");
            }
        }
    }
}
