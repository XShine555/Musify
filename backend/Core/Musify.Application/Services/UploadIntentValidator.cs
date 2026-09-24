using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Services;

public sealed class UploadIntentValidator(
    IDatabase database,
    IStorageService storageService)
{
    public async Task<ErrorOr<Success>> CheckQuotaAsync(
        UploadIntentConfiguration config,
        long userId,
        long requiredBytes,
        int requiredIntentCount,
        CancellationToken cancellationToken)
    {
        var activeIntents = await database.UploadIntents
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.Status == UploadIntentStatus.Issued)
            .ToListAsync(cancellationToken);

        if (activeIntents.Count + requiredIntentCount > config.MaxActiveUploadIntentsPerUser)
            return Error.Validation(description: "Upload intent limit exceeded. Wait for existing uploads to complete or expire.");

        var activeBytes = activeIntents.Sum(i => i.ExpectedSizeBytes ?? config.DefaultExpectedPictureSizeBytes);
        if (activeBytes + requiredBytes > config.MaxActiveUploadBytesPerUser)
            return Error.Validation(description: "Upload byte quota exceeded. Wait for existing uploads to complete or expire.");

        return new Success();
    }

    public async Task<ErrorOr<UploadIntent>> ValidateAndLoadAsync(
        UploadIntentConfiguration config,
        Guid intentId,
        long userId,
        CancellationToken cancellationToken)
    {
        var intent = await database.UploadIntents
            .FirstOrDefaultAsync(i => i.Id == intentId, cancellationToken);

        if (intent == null || intent.UserId != userId)
            return Error.NotFound(description: "Upload intent not found or not accessible.");

        if (intent.IsConsumed)
            return Error.Conflict(description: "Upload intent has already been consumed.");

        if (intent.IsExpired)
            return Error.Validation(description: "Upload intent has expired.");

        var metadata = await storageService.HeadObjectAsync(intent.Bucket, intent.Key, cancellationToken);
        if (metadata == null)
            return Error.NotFound(description: "Uploaded object not found in storage. Upload the file first.");

        if (metadata.ContentLength > config.MaxUploadBytes)
            return Error.Validation(description: $"Uploaded file exceeds the maximum allowed size of {config.MaxUploadBytes} bytes.");

        return intent;
    }
}
