using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Services
{
    public sealed class UploadIntentValidator(
        IDatabase database,
        IStorageService storageService,
        UploadIntentConfiguration config)
    {
        public async Task<ErrorOr<Success>> CheckQuotaAsync(
            long userId,
            long requiredBytes,
            int requiredIntentCount,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var pictureDefault = config.DefaultExpectedPictureSizeBytes;
            var audioDefault = config.DefaultExpectedAudioSizeBytes;

            var active = await database.UploadIntents
                .AsNoTracking()
                .Where(intent => intent.UserId == userId && intent.Status == UploadIntentStatus.Issued && intent.ExpiresAt > now)
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Count = group.Count(),
                    Bytes = group.Sum(intent => intent.ExpectedSizeBytes
                        ?? (intent.Purpose == UploadIntentPurpose.TrackAudio ? audioDefault : pictureDefault))
                })
                .SingleOrDefaultAsync(cancellationToken);

            if ((active?.Count ?? 0) + requiredIntentCount > config.MaxActiveUploadIntentsPerUser)
                return Error.Validation(description: "Upload intent limit exceeded. Wait for existing uploads to complete or expire.");

            if ((active?.Bytes ?? 0) + requiredBytes > config.MaxActiveUploadBytesPerUser)
                return Error.Validation(description: "Upload byte quota exceeded. Wait for existing uploads to complete or expire.");

            return Result.Success;
        }

        public async Task<ErrorOr<UploadIntent>> ValidateAndLoadAsync(
            Guid intentId,
            long userId,
            UploadIntentPurpose purpose,
            CancellationToken cancellationToken)
        {
            var intent = await database.UploadIntents
                .FirstOrDefaultAsync(i => i.Id == intentId, cancellationToken);

            if (intent == null || intent.UserId != userId)
                return AppErrors.NotFound("UploadIntent", intentId);

            if (intent.Purpose != purpose)
                return Error.Validation(description: "Upload intent was issued for a different purpose.");

            if (intent.IsConsumed)
                return AppErrors.Conflict("UploadIntent.Consumed", "Upload intent has already been consumed.");

            if (intent.IsExpiredAt(DateTime.UtcNow))
                return Error.Validation(description: "Upload intent has expired.");

            var metadata = await storageService.HeadObjectAsync(intent.Bucket, intent.Key, cancellationToken);
            if (metadata == null)
                return Error.NotFound("UploadIntent.ObjectMissing", "Uploaded object not found in storage. Upload the file first.");

            if (metadata.ContentLength > config.MaxUploadBytes)
                return Error.Validation(description: $"Uploaded file exceeds the maximum allowed size of {config.MaxUploadBytes} bytes.");

            return intent;
        }
    }
}
