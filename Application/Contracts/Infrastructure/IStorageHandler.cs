using Ardalis.Result;

namespace Musify.Application.Contracts.Infrastructure
{
    public interface IStorageHandler
    {
        Task<Result<Stream>> GetFileAsync(string bucketName, string keyName, CancellationToken cancellationToken);

        Task<string> GetUrlAsync(string bucketName, string keyName, TimeSpan ExpirationTime, CancellationToken cancellationToken);

        Task<Result<string>> UploadFileAsync(Stream sourceStream, string contentType, string bucketName, string keyName, CancellationToken cancellationToken);

        Task<Result> TransferFilesAsync(string sourceDirectory, string bucketName, string route, CancellationToken cancellationToken);

        Task<Result> RemoveFileAsync(string bucketName, string keyName, CancellationToken cancellationToken);

        Task<Result> CopyFileAsync(string sourceBucketName, string sourceKeyName, string destinationBucketName, string destinationKeyName,
            CancellationToken cancellationToken);
    }
}