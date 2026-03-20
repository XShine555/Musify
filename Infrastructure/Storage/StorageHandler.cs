using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Storage
{
    public class StorageHandler(IDatabase database, IAmazonS3 amazonS3, InfrastructureStorageConfiguration storageClientConfiguration, ILogger<StorageHandler> logger)
        : IStorageHandler
    {
        public async Task<Result<Stream>> GetFileAsync(string bucketName, string keyName, CancellationToken cancellationToken)
        {
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = keyName,
            };

            logger.LogDebug("Getting file from S3 with bucket name {BucketName} and key name {KeyName}", bucketName, keyName);

            try
            {
                var response = await amazonS3.GetObjectAsync(request, cancellationToken);

                logger.LogDebug("Successfully got file from S3 with bucket name {BucketName} and key name {KeyName}", bucketName, keyName);
                return Result.Success(response.ResponseStream);
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Failed to get file from S3 with bucket name {BucketName} and key name {KeyName}", bucketName, keyName);
                return Result.NotFound($"File not found in S3 with bucket name {bucketName} and key name {keyName}");
            }
        }

        public async Task<string> GetUrlAsync(string bucketName, string keyName, TimeSpan expirationTime, CancellationToken cancellationToken)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = keyName,
                Expires = DateTime.UtcNow + expirationTime,
                Protocol = storageClientConfiguration.UseHttp ? Protocol.HTTP : Protocol.HTTPS
            };

            logger.LogDebug("Getting pre-signed URL from S3 with bucket name {BucketName} and key name {KeyName}", bucketName, keyName);
            var url = await amazonS3.GetPreSignedURLAsync(request);

            return url;
        }

        public async Task<Result> RemoveFileAsync(string bucketName, string keyName, CancellationToken cancellationToken)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = keyName,
            };

            logger.LogDebug("Removing file from S3 with bucket name {BucketName} and key name {KeyName}", bucketName, keyName);
            var response = await amazonS3.DeleteObjectAsync(request, cancellationToken);

            return Result.NoContent();
        }

        public async Task<Result> TransferFilesAsync(string sourceDirectory, string bucketName, string route, CancellationToken cancellationToken)
        {
            var trasnsferUtility = new TransferUtility(amazonS3);

            var files = Directory.GetFiles(sourceDirectory, "*");

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string filePath = Path.Combine(sourceDirectory, file);

                var upload = new Upload
                {
                    Id = Guid.NewGuid(),
                    BucketName = bucketName,
                    KeyName = $"{route}/{fileName}",
                    ContentType = "Application/Octet-Stream"
                };
                await database.Uploads.AddAsync(upload, cancellationToken);

                try
                {
                    await trasnsferUtility.UploadAsync(filePath, bucketName, fileName, cancellationToken);
                    upload.State = UploadState.Successful;
                    logger.LogInformation("Successfully transferred file {FileName} to S3 with bucket name {BucketName} and route {Route}", fileName, bucketName, route);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to transfer file {FileName} to S3 with bucket name {BucketName} and route {Route}", fileName, bucketName, route);
                    upload.State = UploadState.Failed;
                }
            }

            await database.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully saved upload records to database for all files transferred to S3 with bucket name {BucketName} and route {Route}", bucketName, route);

            return Result.Success();
        }

        public async Task<Result<string>> UploadFileAsync(Stream sourceStream, string contentType, string bucketName, string keyName, CancellationToken cancellationToken)
        {
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                ContentType = contentType,
                InputStream = sourceStream,
                Key = keyName
            };

            var upload = new Upload
            {
                Id = Guid.NewGuid(),
                BucketName = bucketName,
                KeyName = keyName,
                ContentType = contentType
            };
            await database.Uploads.AddAsync(upload, cancellationToken);

            try
            {
                var response = await amazonS3.PutObjectAsync(request, cancellationToken);
                logger.LogInformation("Successfully uploaded file to S3 with bucket name {BucketName} and key name {KeyName}", bucketName, keyName);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to upload file to S3 with bucket name {BucketName} and key name {KeyName}", bucketName, keyName);
                upload.State = UploadState.Failed;
                return Result.Error($"Failed to upload file to S3 with bucket name {bucketName} and key name {keyName}");
            }

            upload.State = UploadState.Successful;
            await database.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully saved upload record to database with bucket name {BucketName} and key name {KeyName}", bucketName, keyName);

            return Result.Success(keyName);
        }

        public async Task<Result> CopyFileAsync(string sourceBucketName, string sourceKeyName, string destinationBucketName, string destinationKeyName,
            CancellationToken cancellationToken)
        {
            var request = new CopyObjectRequest
            {
                SourceBucket = sourceBucketName,
                SourceKey = sourceKeyName,
                DestinationBucket = destinationBucketName,
                DestinationKey = destinationKeyName
            };

            try
            {
                var response = await amazonS3.CopyObjectAsync(request, cancellationToken);
                logger.LogInformation("Successfully copied file from S3 with bucket name {SourceBucketName} and key name {SourceKeyName} to bucket name {DestinationBucketName} and key name {DestinationKeyName}",
                    sourceBucketName, sourceKeyName, destinationBucketName, destinationKeyName);
                return Result.Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to copy file from S3 with bucket name {SourceBucketName} and key name {SourceKeyName} to bucket name {DestinationBucketName} and key name {DestinationKeyName}", 
                    sourceBucketName, sourceKeyName, destinationBucketName, destinationKeyName);
                return Result.Error($"Failed to copy file from S3 with bucket name {sourceBucketName} and key name {sourceKeyName} to bucket name {destinationBucketName} and key name {destinationKeyName}");
            }
        }
    }
}