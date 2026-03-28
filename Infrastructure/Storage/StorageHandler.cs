using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Ardalis.Result;
using Microsoft.Extensions.Logging;
using MimeMapping;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Storage
{
    public class StorageHandler(IDatabase database, IAmazonS3 amazonS3, InfrastructureStorageConfiguration storageClientConfiguration, ILogger<StorageHandler> logger)
        : IStorageHandler
    {
        public async Task<Result<Stream>> GetFileAsync(string bucket, string key, CancellationToken cancellationToken)
        {
            var request = new GetObjectRequest
            {
                BucketName = bucket,
                Key = key,
            };



            try
            {
                var response = await amazonS3.GetObjectAsync(request, cancellationToken);

                logger.LogDebug("Retrieved file from S3 {Bucket}/{Key}", bucket, key);
                return Result.Success(response.ResponseStream);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to get file from S3 {Bucket}/{Key}", bucket, key);
                return Result.NotFound($"File {bucket}/{key} not found");
            }
        }

        public async Task<string> GetUrlAsync(string bucket, string key, TimeSpan expirationTime, CancellationToken cancellationToken)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Expires = DateTime.UtcNow + expirationTime,
                Protocol = storageClientConfiguration.UseHttp ? Protocol.HTTP : Protocol.HTTPS
            };

            logger.LogDebug("Generating pre-signed URL for {Bucket}/{Key}", bucket, key);
            var url = await amazonS3.GetPreSignedURLAsync(request);

            return url;
        }

        public async Task<Result> RemoveFileAsync(string bucket, string key, CancellationToken cancellationToken)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucket,
                Key = key,
            };

            logger.LogDebug("Removing file from S3 {Bucket}/{Key}", bucket, key);
            var response = await amazonS3.DeleteObjectAsync(request, cancellationToken);

            return Result.NoContent();
        }

        public async Task<Result> TransferFilesAsync(string sourceDirectory, string bucket, string route, CancellationToken cancellationToken)
        {
            var trasnsferUtility = new TransferUtility(amazonS3);

            var files = Directory.GetFiles(sourceDirectory, "*");

            var successCount = 0;
            var failedCount = 0;

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string filePath = Path.Combine(sourceDirectory, file);

                var key = Path.Combine(route, fileName);
                var upload = new Upload
                {
                    Id = Guid.NewGuid(),
                    Bucket = bucket,
                    Key = key,
                    ContentType = MimeUtility.GetMimeMapping(filePath)
                };
                await database.Uploads.AddAsync(upload, cancellationToken);

                try
                {
                    await trasnsferUtility.UploadAsync(filePath, bucket, key, cancellationToken);
                    upload.State = UploadState.Successful;
                    successCount++;
                    logger.LogDebug("Transferred file to S3 {Bucket}/{Key}", bucket, key);
                }
                catch (Exception exception)
                {
                    failedCount++;
                    upload.State = UploadState.Failed;
                    logger.LogError(exception, "Failed to transfer file to S3 {Bucket}/{Key}", bucket, key);
                }
            }

            await database.SaveChangesAsync(cancellationToken);
            logger.LogInformation("File transfer completed to {Bucket}/{Route}. Success: {SuccessCount}, Failed: {FailedCount}", bucket, route, successCount, failedCount);

            return Result.Success();
        }

        public async Task<Result<string>> UploadFileAsync(string filePath, string bucket, string key, CancellationToken cancellationToken)
        {
            try
            {
                using var fileStream = File.OpenRead(filePath);
                var contentType = MimeUtility.GetMimeMapping(filePath);
                return await UploadFileAsync(fileStream, contentType, bucket, key, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to upload file to S3 {Bucket}/{Key}", bucket, key);
                return Result.Error($"Failed to upload to {bucket}/{key}");
            }
        }

        public async Task<Result<string>> UploadFileAsync(Stream sourceStream, string contentType, string bucket, string key, CancellationToken cancellationToken)
        {
            var request = new PutObjectRequest
            {
                BucketName = bucket,
                ContentType = contentType,
                InputStream = sourceStream,
                Key = key
            };

            var upload = new Upload
            {
                Id = Guid.NewGuid(),
                Bucket = bucket,
                Key = key,
                ContentType = contentType
            };
            await database.Uploads.AddAsync(upload, cancellationToken);

            try
            {
                var response = await amazonS3.PutObjectAsync(request, cancellationToken);
                logger.LogDebug("Uploaded file to S3 {Bucket}/{Key}", bucket, key);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to upload file to S3 {Bucket}/{Key}", bucket, key);
                upload.State = UploadState.Failed;
                return Result.Error($"Failed to upload to {bucket}/{key}");
            }

            upload.State = UploadState.Successful;
            await database.SaveChangesAsync(cancellationToken);
            logger.LogDebug("Saved upload record for {Bucket}/{Key}", bucket, key);

            return Result.Success(key);
        }

        public async Task<Result> CopyFileAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey,
            CancellationToken cancellationToken)
        {
            var request = new CopyObjectRequest
            {
                SourceBucket = sourceBucket,
                SourceKey = sourceKey,
                DestinationBucket = destinationBucket,
                DestinationKey = destinationKey
            };

            try
            {
                var response = await amazonS3.CopyObjectAsync(request, cancellationToken);
                logger.LogInformation("Copied file from {SourceBucket}/{SourceKey} to {DestinationBucket}/{DestinationKey}",
                    sourceBucket, sourceKey, destinationBucket, destinationKey);
                return Result.Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to copy file from {SourceBucket}/{SourceKey} to {DestinationBucket}/{DestinationKey}", 
                    sourceBucket, sourceKey, destinationBucket, destinationKey);
                return Result.Error($"Failed to copy from {sourceBucket}/{sourceKey} to {destinationBucket}/{destinationKey}");
            }
        }
    }
}