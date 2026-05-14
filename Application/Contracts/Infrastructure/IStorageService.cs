namespace Musify.Application.Contracts.Infrastructure
{
    public interface IStorageService
    {
        Task<Stream> GetFileAsync(string bucket, string key, CancellationToken cancellationToken);

        // Returns null if the object does not exist
        Task<ObjectMetadata?> HeadObjectAsync(string bucket, string key, CancellationToken cancellationToken);

        Task<string> GetUrlAsync(string bucket, string key, TimeSpan ExpirationTime, CancellationToken cancellationToken);

        Task<string> GetUploadUrlAsync(string bucket, string key, string contentType, TimeSpan expirationTime, CancellationToken cancellationToken);

        Task UploadFileAsync(string filePath, string bucket, string key, CancellationToken cancellationToken);

        Task UploadFileAsync(Stream sourceStream, string contentType, string bucket, string key, CancellationToken cancellationToken);

        Task TransferFilesAsync(string sourceDirectory, string bucket, string route, CancellationToken cancellationToken);

        Task RemoveFileAsync(string bucket, string key, CancellationToken cancellationToken);

        Task CopyFileAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey,
            CancellationToken cancellationToken);

        Task RemoveFolderAsync(string bucket, string folderKey, CancellationToken cancellationToken);

        // Returns (key, lastModifiedUtc) for all objects under the given prefix
        IAsyncEnumerable<(string Key, DateTime LastModifiedUtc)> ListObjectsAsync(string bucket, string prefix, CancellationToken cancellationToken);
    }
}