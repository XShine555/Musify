namespace Musify.Application.Contracts;

public interface IStorageService
{
    /// <summary>Opens an object for reading, or returns null when it does not exist.</summary>
    public Task<Stream?> GetFileAsync(string bucket, string key, CancellationToken cancellationToken);

    public Task<ObjectMetaData?> HeadObjectAsync(string bucket, string key, CancellationToken cancellationToken);

    public Task<string> GetUploadUrlAsync(string bucket, string key, string contentType, TimeSpan expirationTime, bool preventOverwrite = true, CancellationToken cancellationToken = default);

    public Task UploadFileAsync(string filePath, string bucket, string key, CancellationToken cancellationToken);

    public Task UploadFileAsync(Stream sourceStream, string contentType, string bucket, string key, CancellationToken cancellationToken);

    public Task<IReadOnlyList<string>> TransferFilesAsync(string sourceDirectory, string bucket, string route, CancellationToken cancellationToken);

    public Task RemoveFileAsync(string bucket, string key, CancellationToken cancellationToken);

    public Task CopyFileAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey,
        CancellationToken cancellationToken);

    public IAsyncEnumerable<StorageObject> ListObjectsAsync(string bucket, string prefix, CancellationToken cancellationToken);
}
