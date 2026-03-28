using Ardalis.Result;

namespace Musify.Application.Contracts.Infrastructure
{
    public interface IStorageHandler
    {
        Task<Result<Stream>> GetFileAsync(string bucket, string key, CancellationToken cancellationToken);

        Task<string> GetUrlAsync(string bucket, string key, TimeSpan ExpirationTime, CancellationToken cancellationToken);

        Task<Result<string>> UploadFileAsync(string filePath, string bucket, string key, CancellationToken cancellationToken);

        Task<Result<string>> UploadFileAsync(Stream sourceStream, string contentType, string bucket, string key, CancellationToken cancellationToken);

        Task<Result> TransferFilesAsync(string sourceDirectory, string bucket, string route, CancellationToken cancellationToken);

        Task<Result> RemoveFileAsync(string bucket, string key, CancellationToken cancellationToken);

        Task<Result> CopyFileAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey,
            CancellationToken cancellationToken);
    }
}