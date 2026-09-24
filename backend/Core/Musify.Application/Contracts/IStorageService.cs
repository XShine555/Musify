namespace Musify.Application.Contracts;

public interface IStorageService
{
    public Task<Stream> GetFileAsync(string bucket, string key, CancellationToken cancellationToken);

    public Task<ObjectMetaData?> HeadObjectAsync(string bucket, string key, CancellationToken cancellationToken);

    public Task<string> GetUrlAsync(string bucket, string key, TimeSpan ExpirationTime, CancellationToken cancellationToken);

    public Task<string> GetUploadUrlAsync(string bucket, string key, string contentType, TimeSpan expirationTime, CancellationToken cancellationToken, bool preventOverwrite = true);

    public Task UploadFileAsync(string filePath, string bucket, string key, CancellationToken cancellationToken);

    public Task UploadFileAsync(Stream sourceStream, string contentType, string bucket, string key, CancellationToken cancellationToken);

    public Task<IReadOnlyList<string>> TransferFilesAsync(string sourceDirectory, string bucket, string route, CancellationToken cancellationToken);

    public Task RemoveFileAsync(string bucket, string key, CancellationToken cancellationToken);

    public Task CopyFileAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey,
        CancellationToken cancellationToken);

    public Task RemoveFolderAsync(string bucket, string folderKey, CancellationToken cancellationToken);

    public IAsyncEnumerable<StorageObject> ListObjectsAsync(string bucket, string prefix, CancellationToken cancellationToken);
}
