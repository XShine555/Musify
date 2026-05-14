using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Configuration;
using Musify.Domain.Entities;

namespace Musify.Application.UploadIntents
{
    internal static class UploadIntentHelpers
    {
        internal static async Task<IntentValidationResult> ValidateAndLoadAsync(
            IDatabase database,
            IStorageService storageService,
            UploadIntentConfiguration config,
            Guid intentId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var intent = await database.UploadIntents
                .FirstOrDefaultAsync(i => i.Id == intentId, cancellationToken);

            if (intent is null || intent.UserId != userId)
                return IntentValidationResult.Fail(Result.NotFound("Upload intent not found or not accessible."));

            if (intent.Status == UploadIntentStatus.Consumed)
                return IntentValidationResult.Fail(Result.Conflict("Upload intent has already been consumed."));

            if (intent.Status == UploadIntentStatus.Expired || intent.ExpiresAt < DateTime.UtcNow)
                return IntentValidationResult.Fail(Result.Invalid(new ValidationError("Upload intent has expired.")));

            var metadata = await storageService.HeadObjectAsync(intent.Bucket, intent.Key, cancellationToken);
            if (metadata is null)
                return IntentValidationResult.Fail(Result.NotFound("Uploaded object not found in storage. Upload the file first."));

            if (metadata.ContentLength > config.MaxUploadBytes)
                return IntentValidationResult.Fail(Result.Invalid(new ValidationError($"Uploaded file exceeds the maximum allowed size of {config.MaxUploadBytes} bytes.")));

            return IntentValidationResult.Ok(intent);
        }

        internal static async Task<Result> CheckQuotaAsync(
            IDatabase database,
            UploadIntentConfiguration config,
            ILogger logger,
            Guid userId,
            long requiredBytes,
            int requiredIntentCount,
            CancellationToken cancellationToken)
        {
            var activeIntents = await database.UploadIntents
                .AsNoTracking()
                .Where(i => i.UserId == userId && i.Status == UploadIntentStatus.Issued)
                .ToListAsync(cancellationToken);

            if (activeIntents.Count + requiredIntentCount > config.MaxActiveUploadIntentsPerUser)
            {
                logger.LogWarning("User {UserId} exceeded max active upload intents ({Max})", userId, config.MaxActiveUploadIntentsPerUser);
                return Result.Invalid(new ValidationError("Upload intent limit exceeded. Wait for existing uploads to complete or expire."));
            }

            var activeBytes = activeIntents.Sum(i => i.ExpectedSizeBytes ?? config.DefaultExpectedPictureSizeBytes);
            if (activeBytes + requiredBytes > config.MaxActiveUploadBytesPerUser)
            {
                logger.LogWarning("User {UserId} exceeded max active upload bytes ({Max})", userId, config.MaxActiveUploadBytesPerUser);
                return Result.Invalid(new ValidationError("Upload byte quota exceeded. Wait for existing uploads to complete or expire."));
            }

            return Result.Success();
        }

        internal static Result<string> ValidateExtension(string fileType, string[] allowedExtensions)
        {
            var extension = fileType.Trim();
            if (!extension.StartsWith('.'))
                extension = "." + extension;

            extension = extension.ToLowerInvariant();

            if (extension.Contains('/') || extension.Contains('\\'))
                return Result.Error("Invalid FileType");

            if (!allowedExtensions.Contains(extension))
                return Result.Error($"Unsupported FileType '{extension}'");

            return Result.Success(extension);
        }
    }
}
