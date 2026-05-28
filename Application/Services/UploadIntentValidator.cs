using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Application.Contracts;

namespace Musify.Application.Services;

public sealed class UploadIntentValidator(
    IDatabase database,
    IStorageService storageService)
{
    public async Task<Result> CheckQuotaAsync(
        UploadIntentConfiguration config,
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
            return Result.Invalid(new ValidationError("Upload intent limit exceeded. Wait for existing uploads to complete or expire."));

        var activeBytes = activeIntents.Sum(i => i.ExpectedSizeBytes ?? config.DefaultExpectedPictureSizeBytes);
        if (activeBytes + requiredBytes > config.MaxActiveUploadBytesPerUser)
            return Result.Invalid(new ValidationError("Upload byte quota exceeded. Wait for existing uploads to complete or expire."));

        return Result.Success();
    }

    public async Task<Result<UploadIntent>> ValidateAndLoadAsync(
        UploadIntentConfiguration config,
        Guid intentId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var intent = await database.UploadIntents
            .FirstOrDefaultAsync(i => i.Id == intentId, cancellationToken);

        if (intent is null || intent.UserId != userId)
            return Result.NotFound("Upload intent not found or not accessible.");

        if (intent.Status == UploadIntentStatus.Consumed)
            return Result.Conflict("Upload intent has already been consumed.");

        if (intent.Status == UploadIntentStatus.Expired || intent.ExpiresAt < DateTime.UtcNow)
            return Result.Invalid(new ValidationError("Upload intent has expired."));

        var metadata = await storageService.HeadObjectAsync(intent.Bucket, intent.Key, cancellationToken);
        if (metadata is null)
            return Result.NotFound("Uploaded object not found in storage. Upload the file first.");

        if (metadata.ContentLength > config.MaxUploadBytes)
            return Result.Invalid(new ValidationError($"Uploaded file exceeds the maximum allowed size of {config.MaxUploadBytes} bytes."));

        return Result.Success(intent);
    }
}
