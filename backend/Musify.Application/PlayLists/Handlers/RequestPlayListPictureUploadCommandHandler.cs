using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Services;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Application.Contracts;

namespace Musify.Application.PlayLists.Handlers
{
    public class RequestPlayListPictureUploadCommandHandler(
        IDatabase database,
        UploadIntentValidator uploadIntentValidator,
        IStorageService storageService,
        ILogger<RequestPlayListPictureUploadCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : ICommandHandler<RequestPlayListPictureUploadCommand, ErrorOr<PlayListPictureUploadResponse>>
    {
        public async ValueTask<ErrorOr<PlayListPictureUploadResponse>> Handle(RequestPlayListPictureUploadCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Error.NotFound(description: $"User {request.UserId} not found");
            }

            var effectiveSizeBytes = request.ExpectedSizeBytes
                ?? uploadIntentConfiguration.DefaultExpectedPictureSizeBytes;

            var objectName = $"{Guid.NewGuid() }.{request.FileType.TrimStart('.').ToLowerInvariant() }";

            var tempKey = playListConfiguration.Routes.BuildTempPicturePath(
                uploadIntentConfiguration.TemporalRootPrefix, request.UserId, objectName);

            try
            {
                var uploadUrl = await storageService.GetUploadUrlAsync(
                    storageConfiguration.Bucket,
                    tempKey,
                    request.ContentType,
                    TimeSpan.FromSeconds(uploadIntentConfiguration.UploadUrlExpiresInSeconds),
                    cancellationToken);

                await using var transaction = await database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable, cancellationToken);

                var quotaCheck = await uploadIntentValidator.CheckQuotaAsync(
                    uploadIntentConfiguration,
                    request.UserId, effectiveSizeBytes, 1, cancellationToken);
                if (quotaCheck.IsError)
                {
                    logger.LogWarning("User {UserId} failed upload intent quota check", request.UserId);
                    return quotaCheck.Errors;
                }

                var intent = new UploadIntent
                {
                    UserId = request.UserId,
                    Bucket = storageConfiguration.Bucket,
                    Key = tempKey,
                    ObjectName = objectName,
                    ContentType = request.ContentType,
                    ExpectedSizeBytes = effectiveSizeBytes,
                    Purpose = UploadIntentPurpose.PlayListPicture,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(uploadIntentConfiguration.UploadUrlExpiresInSeconds),
                };

                await database.UploadIntents.AddAsync(intent, cancellationToken);
                await database.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation("Issued upload intent {IntentId} for user {UserId} (PlayListPicture, temp key: {Key})", intent.Id, request.UserId, tempKey);

                return new PlayListPictureUploadResponse(
                    intent.Id,
                    storageConfiguration.Bucket,
                    tempKey,
                    objectName,
                    request.ContentType,
                    uploadIntentConfiguration.UploadUrlExpiresInSeconds,
                    uploadUrl);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to generate playlist picture upload URL for user {UserId}", request.UserId);
                return Error.Failure(description: "Failed to generate upload URL");
            }
        }
    }
}
