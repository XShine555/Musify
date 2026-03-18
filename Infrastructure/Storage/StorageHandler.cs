using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;

namespace Musify.Infrastructure.Storage
{
    public class StorageHandler(IAmazonS3 amazonS3, ILogger<StorageHandler> logger)
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
            catch
            {
                logger.LogDebug("Failed to get file from S3 with bucket name {BucketName} and key name {KeyName}", bucketName, keyName);
                return Result.NotFound();
            }
        }

        public async Task<string> GetUrlAsync(string bucketName, string keyName, TimeSpan expirationTime, CancellationToken cancellationToken)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = keyName,
                Expires = DateTime.UtcNow + expirationTime,
                Protocol = Protocol.HTTP
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

                try
                {
                    await trasnsferUtility.UploadAsync(filePath, bucketName, fileName, cancellationToken);
                    logger.LogInformation("Successfully transferred file {FileName} to S3 with bucket name {BucketName} and route {Route}", fileName, bucketName, route);
                }
                catch
                {
                    logger.LogError("Failed to transfer file {FileName} to S3 with bucket name {BucketName} and route {Route}", fileName, bucketName, route);
                    return Result.Error($"Failed to transfer file {fileName} to S3 with bucket name {bucketName} and route {route}");
                }
            }

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

            try
            {
                var response = await amazonS3.PutObjectAsync(request, cancellationToken);
                logger.LogInformation("Successfully uploaded file to S3 with bucket name {BucketName} and key name {KeyName}", bucketName, keyName);
            }
            catch
            {
                logger.LogError("Failed to upload file to S3 with bucket name {BucketName} and key name {KeyName}", bucketName, keyName);
                return Result.Error($"Failed to upload file to S3 with bucket name {bucketName} and key name {keyName}");
            }

            return Result.Success(keyName);
        }
    }
}