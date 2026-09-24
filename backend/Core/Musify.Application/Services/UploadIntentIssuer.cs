using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Pictures.Responses;
using Musify.Application.Shared;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Services
{
    public sealed record UploadRequest(
        UploadIntentPurpose Purpose,
        PictureRoutes Routes,
        string FileType,
        string ContentType,
        long? ExpectedSizeBytes);

    public sealed record IssuedUpload(
        Guid IntentId,
        string Key,
        string ObjectName,
        string ContentType,
        string UploadUrl);

    /// <summary>Issues pre-signed upload URLs and the upload intents that track them, checking the user quota first.</summary>
    public sealed class UploadIntentIssuer(
        IDatabase database,
        IStorageService storageService,
        UploadIntentValidator uploadIntentValidator,
        ApplicationStorageConfiguration storageConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration,
        ILogger<UploadIntentIssuer> logger)
    {
        public async Task<ErrorOr<IReadOnlyList<IssuedUpload>>> IssueAsync(
            long userId,
            IReadOnlyList<UploadRequest> requests,
            CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(user => user.Id == userId, cancellationToken);
            if (!userExists)
                return AppErrors.NotFound("User", userId);

            var planned = requests
                .Select(request =>
                {
                    var objectName = $"{Guid.NewGuid()}.{request.FileType.TrimStart('.').ToLowerInvariant()}";
                    var key = request.Routes.BuildTempPath(uploadIntentConfiguration.TempRootPrefix, userId, objectName);
                    var size = request.ExpectedSizeBytes ?? DefaultSize(request.Purpose);
                    return (Request: request, ObjectName: objectName, Key: key, Size: size);
                })
                .ToList();

            await using var transaction = await database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);

            var quota = await uploadIntentValidator.CheckQuotaAsync(
                userId, planned.Sum(item => item.Size), planned.Count, cancellationToken);
            if (quota.IsError)
                return quota.Errors;

            var expiresIn = TimeSpan.FromSeconds(uploadIntentConfiguration.UploadUrlExpiresInSeconds);
            var expiresAt = DateTime.UtcNow + expiresIn;
            var issued = new List<IssuedUpload>(planned.Count);

            foreach (var item in planned)
            {
                var uploadUrl = await storageService.GetUploadUrlAsync(
                    storageConfiguration.Bucket, item.Key, item.Request.ContentType, expiresIn, cancellationToken: cancellationToken);

                var intent = new UploadIntent
                {
                    UserId = userId,
                    Bucket = storageConfiguration.Bucket,
                    Key = item.Key,
                    ObjectName = item.ObjectName,
                    ContentType = item.Request.ContentType,
                    ExpectedSizeBytes = item.Size,
                    Purpose = item.Request.Purpose,
                    ExpiresAt = expiresAt,
                };

                await database.UploadIntents.AddAsync(intent, cancellationToken);
                issued.Add(new IssuedUpload(intent.Id, item.Key, item.ObjectName, item.Request.ContentType, uploadUrl));
            }

            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Issued {Count} upload intents for user {UserId} ({Purposes})",
                issued.Count, userId, string.Join('/', planned.Select(item => item.Request.Purpose)));

            return issued;
        }

        public async Task<ErrorOr<IssuedUpload>> IssueOneAsync(long userId, UploadRequest request, CancellationToken cancellationToken)
        {
            var issued = await IssueAsync(userId, [request], cancellationToken);
            return issued.IsError ? issued.Errors : issued.Value[0];
        }

        public async Task<ErrorOr<PictureUploadResponse>> IssuePictureAsync(
            long userId,
            UploadIntentPurpose purpose,
            IPictureOwnerConfiguration owner,
            string fileType,
            string contentType,
            long? expectedSizeBytes,
            CancellationToken cancellationToken)
        {
            var issued = await IssueOneAsync(
                userId, new UploadRequest(purpose, owner.Routes, fileType, contentType, expectedSizeBytes), cancellationToken);
            if (issued.IsError)
                return issued.Errors;

            return new PictureUploadResponse(
                issued.Value.IntentId,
                storageConfiguration.Bucket,
                issued.Value.Key,
                issued.Value.ObjectName,
                issued.Value.ContentType,
                uploadIntentConfiguration.UploadUrlExpiresInSeconds,
                issued.Value.UploadUrl);
        }

        private long DefaultSize(UploadIntentPurpose purpose) =>
            purpose == UploadIntentPurpose.TrackAudio
                ? uploadIntentConfiguration.DefaultExpectedAudioSizeBytes
                : uploadIntentConfiguration.DefaultExpectedPictureSizeBytes;
    }
}
