using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Configuration;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Responses;
using Musify.Application.UploadIntents;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Handlers
{
    public class RequestPlayListPictureUploadCommandHandler(
        IDatabase database,
        IStorageService storageService,
        ILogger<RequestPlayListPictureUploadCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : ICommandHandler<RequestPlayListPictureUploadCommand, Result<PlayListPictureUploadResponse>>
    {
        static readonly string[] AllowedExtensions = [".webp", ".png", ".jpg", ".jpeg"];

        public async ValueTask<Result<PlayListPictureUploadResponse>> Handle(RequestPlayListPictureUploadCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Result.NotFound($"User {request.UserId} not found");
            }

            var extensionResult = UploadIntentHelpers.ValidateExtension(request.FileType, AllowedExtensions);
            if (!extensionResult.IsSuccess)
                return Result.Invalid(extensionResult.Errors.Select(error => new ValidationError(error)).ToArray());

            var effectiveSizeBytes = request.ExpectedSizeBytes
                ?? uploadIntentConfiguration.DefaultExpectedPictureSizeBytes;

            var objectName = Guid.NewGuid() + extensionResult.Value;

            var tempKey = playListConfiguration.Routes.BuildTempPicturePath(
                uploadIntentConfiguration.TempRootPrefix, request.UserId, objectName);

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

                var quotaCheck = await UploadIntentHelpers.CheckQuotaAsync(
                    database, uploadIntentConfiguration, logger,
                    request.UserId, effectiveSizeBytes, 1, cancellationToken);
                if (!quotaCheck.IsSuccess)
                    return quotaCheck;

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

                return Result.Success(new PlayListPictureUploadResponse(
                    intent.Id,
                    storageConfiguration.Bucket,
                    tempKey,
                    objectName,
                    request.ContentType,
                    uploadIntentConfiguration.UploadUrlExpiresInSeconds,
                    uploadUrl));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to generate playlist picture upload URL for user {UserId}", request.UserId);
                return Result.Error("Failed to generate upload URL");
            }
        }
    }
}
