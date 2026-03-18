using Ardalis.Result;

namespace Musify.Application.Contracts.Infrastructure
{
    public interface IStorageHandler
    {
        Task<Result<Stream>> GetFile(string bucketName, string keyName, CancellationToken cancellationToken);

        Task<string> GetUrl(string bucketName, string keyName, TimeSpan ExpirationTime, CancellationToken cancellationToken);

        Task<Result<string>> UploadFile(Stream sourceStream, string contentType, string bucketName, string keyName, CancellationToken cancellationToken);

        Task<Result> TransferFiles(string sourceDirectory, string bucketName, string route, CancellationToken cancellationToken);

        Task<Result> RemoveFile(string bucketName, string keyName, CancellationToken cancellationToken);
    }
}