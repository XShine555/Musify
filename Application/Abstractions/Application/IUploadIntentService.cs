using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Domain.Entities;

namespace Musify.Application.Abstractions.Application;

public interface IUploadIntentService
{
    Task<Result> CheckQuotaAsync(
        UploadIntentConfiguration config,
        ILogger logger,
        Guid userId,
        long requiredBytes,
        int requiredIntentCount,
        CancellationToken cancellationToken);

    Task<Result<UploadIntent>> ValidateAndLoadAsync(
        UploadIntentConfiguration config,
        Guid intentId,
        Guid userId,
        CancellationToken cancellationToken);
}
