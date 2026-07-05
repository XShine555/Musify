using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using MimeMapping;
using Musify.Application.Contracts;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.Configuration;
using ObjectMetaData = Musify.Application.Contracts.ObjectMetaData;

namespace Musify.Infrastructure.Services
{
    public class StorageService(IDatabase database, IAmazonS3 amazonS3, ILogger<StorageService> logger,
        InfrastructureStorageConfiguration storageClientConfiguration)
        : IStorageService
    {
        public async Task<Stream> GetFileAsync(string bucket, string key, CancellationToken cancellationToken)
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
                return response.ResponseStream;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to get file from S3 {Bucket}/{Key}", bucket, key);
                throw;
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
            return await amazonS3.GetPreSignedURLAsync(request);
        }

        public async Task<string> GetUploadUrlAsync(
            string bucket,
            string key,
            string contentType,
            TimeSpan expirationTime,
            CancellationToken cancellationToken,
            bool preventOverwrite = true)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Expires = DateTime.UtcNow + expirationTime,
                Protocol = storageClientConfiguration.UseHttp ? Protocol.HTTP : Protocol.HTTPS,
                Verb = HttpVerb.PUT,
                ContentType = contentType
            };

            if (preventOverwrite)
                request.Headers["If-None-Match"] = "*";

            logger.LogDebug("Generating pre-signed upload URL for {Bucket}/{Key}", bucket, key);
            return await amazonS3.GetPreSignedURLAsync(request);
        }

        public async Task<ObjectMetaData?> HeadObjectAsync(string bucket, string key, CancellationToken cancellationToken)
        {
            try
            {
                var response = await amazonS3.GetObjectMetadataAsync(bucket, key, cancellationToken);
                return new ObjectMetaData(response.Headers.ContentType, response.ContentLength);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "HEAD failed for {Bucket}/{Key}", bucket, key);
                throw;
            }
        }

        public async Task RemoveFileAsync(string bucket, string key, CancellationToken cancellationToken)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucket,
                Key = key,
            };

            logger.LogDebug("Removing file from S3 {Bucket}/{Key}", bucket, key);
            await amazonS3.DeleteObjectAsync(request, cancellationToken);
        }

        public async Task<IReadOnlyList<string>> TransferFilesAsync(string sourceDirectory, string bucket, string route, CancellationToken cancellationToken)
        {
            var trasnsferUtility = new TransferUtility(amazonS3);

            var files = Directory.GetFiles(sourceDirectory, "*");

            var uploadedKeys = new List<string>();
            var successCount = 0;
            var failedCount = 0;

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string filePath = Path.Combine(sourceDirectory, file);

                var key = string.Join('/', new[] { route, fileName }
                    .Where(static s => !string.IsNullOrWhiteSpace(s))
                    .Select(static s => s.Trim().Trim('/', '\\')));
                var contentType = MimeUtility.GetMimeMapping(filePath);
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
                    await trasnsferUtility.UploadAsync(
                        new TransferUtilityUploadRequest
                        {
                            FilePath = filePath,
                            BucketName = bucket,
                            Key = key,
                            ContentType = contentType
                        },
                        cancellationToken);
                    upload.State = UploadState.Successful;
                    uploadedKeys.Add(key);
                    successCount++;
                    logger.LogDebug("Transferred file to S3 {Bucket}/{Key}", bucket, key);
                }
                catch (Exception exception)
                {
                    failedCount++;
                    upload.State = UploadState.Failed;
                    logger.LogError(exception, "Failed to transfer file to S3 {Bucket}/{Key}", bucket, key);
                    throw;
                }
            }

            await database.SaveChangesAsync(cancellationToken);
            logger.LogInformation("File transfer completed to {Bucket}/{Route}. Success: {SuccessCount}, Failed: {FailedCount}", bucket, route, successCount, failedCount);

            return uploadedKeys;
        }

        public async Task UploadFileAsync(string filePath, string bucket, string key, CancellationToken cancellationToken)
        {
            try
            {
                using var fileStream = File.OpenRead(filePath);
                var contentType = MimeUtility.GetMimeMapping(filePath);
                await UploadFileAsync(fileStream, contentType, bucket, key, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to upload file to S3 {Bucket}/{Key}", bucket, key);
                throw;
            }
        }

        public async Task UploadFileAsync(Stream sourceStream, string contentType, string bucket, string key, CancellationToken cancellationToken)
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
                await database.SaveChangesAsync(cancellationToken);
                throw;
            }

            upload.State = UploadState.Successful;
            await database.SaveChangesAsync(cancellationToken);
            logger.LogDebug("Saved upload record for {Bucket}/{Key}", bucket, key);
        }

        public async Task CopyFileAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey,
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
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to copy file from {SourceBucket}/{SourceKey} to {DestinationBucket}/{DestinationKey}", 
                    sourceBucket, sourceKey, destinationBucket, destinationKey);
                throw;
            }
        }

        public async IAsyncEnumerable<StorageObject> ListObjectsAsync(
            string bucket, string prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = prefix.EndsWith('/') ? prefix : $"{prefix}/"
            };

            ListObjectsV2Response listResponse;
            do
            {
                listResponse = await amazonS3.ListObjectsV2Async(listRequest, cancellationToken);
                foreach (var s3Object in listResponse.S3Objects ?? [] )
                    yield return new StorageObject(s3Object.Key, (s3Object.LastModified ?? DateTime.UtcNow).ToUniversalTime());

                listRequest.ContinuationToken = listResponse.NextContinuationToken;
            }
            while (listResponse.IsTruncated.HasValue && listResponse.IsTruncated.Value);
        }

        public async Task RemoveFolderAsync(string bucket, string folderKey, CancellationToken cancellationToken)
        {
            var prefix = folderKey.EndsWith('/') ? folderKey : $"{folderKey}/";
            var listRequest = new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = prefix
            };

            ListObjectsV2Response listResponse;
            do
            {
                listResponse = await amazonS3.ListObjectsV2Async(listRequest, cancellationToken);

                if (listResponse.S3Objects is null or { Count: 0 } )
                    break;

                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = bucket,
                    Objects = listResponse.S3Objects
                        .Select(o => new KeyVersion { Key = o.Key } )
                        .ToList()
                };

                var deleteResponse = await amazonS3.DeleteObjectsAsync(deleteRequest, cancellationToken);

                if (deleteResponse.DeleteErrors.Count > 0)
                {
                    var errors = string.Join(", ", deleteResponse.DeleteErrors.Select(e => $"{e.Key}: {e.Message}"));
                    throw new InvalidOperationException($"Failed to delete some objects in folder {folderKey}: {errors}");
                }

                listRequest.ContinuationToken = listResponse.NextContinuationToken;

            } while (listResponse.IsTruncated.HasValue && listResponse.IsTruncated.Value);
        }
    }
}