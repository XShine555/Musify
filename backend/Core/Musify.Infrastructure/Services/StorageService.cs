using System.Runtime.CompilerServices;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using MimeMapping;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Services
{
    public sealed class StorageService(
        IAmazonS3 amazonS3,
        ILogger<StorageService> logger,
        InfrastructureStorageConfiguration storageClientConfiguration)
        : IStorageService
    {
        public async Task<Stream?> GetFileAsync(string bucket, string key, CancellationToken cancellationToken)
        {
            try
            {
                var response = await amazonS3.GetObjectAsync(
                    new GetObjectRequest { BucketName = bucket, Key = key }, cancellationToken);
                logger.LogDebug("Retrieved file from S3 {Bucket}/{Key}", bucket, key);
                return response.ResponseStream;
            }
            catch (AmazonS3Exception exception) when (exception.ErrorCode == "NoSuchKey")
            {
                logger.LogDebug("File not found in S3 {Bucket}/{Key}", bucket, key);
                return null;
            }
        }

        public async Task<string> GetUploadUrlAsync(
            string bucket,
            string key,
            string contentType,
            TimeSpan expirationTime,
            bool preventOverwrite = true,
            CancellationToken cancellationToken = default)
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
            catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task RemoveFileAsync(string bucket, string key, CancellationToken cancellationToken)
        {
            logger.LogDebug("Removing file from S3 {Bucket}/{Key}", bucket, key);
            await amazonS3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = key }, cancellationToken);
        }

        public async Task<IReadOnlyList<string>> TransferFilesAsync(
            string sourceDirectory, string bucket, string route, CancellationToken cancellationToken)
        {
            using var transferUtility = new TransferUtility(amazonS3);

            var uploadedKeys = new List<string>();

            foreach (var filePath in Directory.GetFiles(sourceDirectory, "*"))
            {
                var key = StorageKey.Combine(route, Path.GetFileName(filePath));

                await transferUtility.UploadAsync(
                    new TransferUtilityUploadRequest
                    {
                        FilePath = filePath,
                        BucketName = bucket,
                        Key = key,
                        ContentType = MimeUtility.GetMimeMapping(filePath)
                    },
                    cancellationToken);

                uploadedKeys.Add(key);
                logger.LogDebug("Transferred file to S3 {Bucket}/{Key}", bucket, key);
            }

            logger.LogInformation("Transferred {Count} files to {Bucket}/{Route}", uploadedKeys.Count, bucket, route);

            return uploadedKeys;
        }

        public async Task UploadFileAsync(string filePath, string bucket, string key, CancellationToken cancellationToken)
        {
            await using var fileStream = File.OpenRead(filePath);
            await UploadFileAsync(fileStream, MimeUtility.GetMimeMapping(filePath), bucket, key, cancellationToken);
        }

        public async Task UploadFileAsync(Stream sourceStream, string contentType, string bucket, string key, CancellationToken cancellationToken)
        {
            await amazonS3.PutObjectAsync(
                new PutObjectRequest { BucketName = bucket, ContentType = contentType, InputStream = sourceStream, Key = key },
                cancellationToken);
            logger.LogDebug("Uploaded file to S3 {Bucket}/{Key}", bucket, key);
        }

        public async Task CopyFileAsync(
            string sourceBucket, string sourceKey, string destinationBucket, string destinationKey, CancellationToken cancellationToken)
        {
            await amazonS3.CopyObjectAsync(
                new CopyObjectRequest
                {
                    SourceBucket = sourceBucket,
                    SourceKey = sourceKey,
                    DestinationBucket = destinationBucket,
                    DestinationKey = destinationKey
                },
                cancellationToken);

            logger.LogDebug("Copied file from {SourceBucket}/{SourceKey} to {DestinationBucket}/{DestinationKey}",
                sourceBucket, sourceKey, destinationBucket, destinationKey);
        }

        public async IAsyncEnumerable<StorageObject> ListObjectsAsync(
            string bucket, string prefix, [EnumeratorCancellation] CancellationToken cancellationToken)
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
                foreach (var s3Object in listResponse.S3Objects ?? [])
                    yield return new StorageObject(s3Object.Key, (s3Object.LastModified ?? DateTime.UtcNow).ToUniversalTime());

                listRequest.ContinuationToken = listResponse.NextContinuationToken;
            }
            while (listResponse.IsTruncated == true);
        }
    }
}
