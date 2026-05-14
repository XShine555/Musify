using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Configuration;
using Musify.Application.Tracks.Commands;
using Musify.Application.Tracks.Responses;
using Musify.Application.UploadIntents;
using Musify.Domain.Entities;

namespace Musify.Application.Tracks.Handler
{
    public class RequestTrackUploadUrlsCommandHandler(
        IDatabase database,
        IStorageService storageService,
        ILogger<RequestTrackUploadUrlsCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        TrackConfiguration trackConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : ICommandHandler<RequestTrackUploadUrlsCommand, Result<TrackUploadUrlsResponse>>
    {
        static readonly string[] AllowedPictureExtensions = [".webp", ".png", ".jpg", ".jpeg"];
        static readonly string[] AllowedAudioExtensions = [".mp3", ".wav", ".m4a", ".flac", ".ogg", ".aac", ".opus"];

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

            var pictureExtensionResult = UploadIntentHelpers.ValidateExtension(request.PictureFileType, AllowedPictureExtensions);
            if (!pictureExtensionResult.IsSuccess)
                return Result.Invalid(pictureExtensionResult.Errors.Select(error => new ValidationError(error)).ToArray());

            var audioExtensionResult = UploadIntentHelpers.ValidateExtension(request.AudioFileType, AllowedAudioExtensions);
            if (!audioExtensionResult.IsSuccess)
                return Result.Invalid(audioExtensionResult.Errors.Select(error => new ValidationError(error)).ToArray());

            var effectivePictureSize = request.ExpectedPictureSizeBytes
                ?? uploadIntentConfiguration.DefaultExpectedPictureSizeBytes;
            var effectiveAudioSize = request.ExpectedAudioSizeBytes
                ?? uploadIntentConfiguration.DefaultExpectedAudioSizeBytes;

            var pictureObjectName = Guid.NewGuid() + pictureExtensionResult.Value;
            var audioObjectName = Guid.NewGuid() + audioExtensionResult.Value;

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

                var quotaCheck = await UploadIntentHelpers.CheckQuotaAsync(
                    database, uploadIntentConfiguration, logger,
                    request.UserId, effectivePictureSize + effectiveAudioSize, 2, cancellationToken);
                if (!quotaCheck.IsSuccess)
                    return quotaCheck;

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
