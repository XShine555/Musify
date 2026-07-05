namespace Musify.Application.Contracts
{
    public interface IStorageService
    {
        Task<Stream> GetFileAsync(string bucket, string key, CancellationToken cancellationToken);

        Task<ObjectMetaData?> HeadObjectAsync(string bucket, string key, CancellationToken cancellationToken);

        Task<string> GetUrlAsync(string bucket, string key, TimeSpan ExpirationTime, CancellationToken cancellationToken);

        Task<string> GetUploadUrlAsync(string bucket, string key, string contentType, TimeSpan expirationTime, CancellationToken cancellationToken, bool preventOverwrite = true);

        Task UploadFileAsync(string filePath, string bucket, string key, CancellationToken cancellationToken);

        Task UploadFileAsync(Stream sourceStream, string contentType, string bucket, string key, CancellationToken cancellationToken);

        Task<IReadOnlyList<string>> TransferFilesAsync(string sourceDirectory, string bucket, string route, CancellationToken cancellationToken);

        Task RemoveFileAsync(string bucket, string key, CancellationToken cancellationToken);

        Task CopyFileAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey,
            CancellationToken cancellationToken);

        Task RemoveFolderAsync(string bucket, string folderKey, CancellationToken cancellationToken);
        
        IAsyncEnumerable<StorageObject> ListObjectsAsync(string bucket, string prefix, CancellationToken cancellationToken);
    }
}