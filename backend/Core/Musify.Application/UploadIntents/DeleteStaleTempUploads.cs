using Mediator;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;

namespace Musify.Application.UploadIntents
{
    /// <summary>Deletes temporary upload objects that nobody claimed within the retention window.</summary>
    public record DeleteStaleTempUploadsCommand : ICommand<int>;

    public class DeleteStaleTempUploadsCommandHandler(
        IStorageService storageService,
        ApplicationStorageConfiguration storageConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration,
        ILogger<DeleteStaleTempUploadsCommandHandler> logger)
        : ICommandHandler<DeleteStaleTempUploadsCommand, int>
    {
        public async ValueTask<int> Handle(DeleteStaleTempUploadsCommand request, CancellationToken cancellationToken)
        {
            var cutOff = DateTime.UtcNow.AddDays(-uploadIntentConfiguration.TempUploadsRetentionDays);
            var deleted = 0;

            await foreach (var (key, lastModified) in storageService.ListObjectsAsync(
                storageConfiguration.Bucket, uploadIntentConfiguration.TempRootPrefix, cancellationToken))
            {
                if (lastModified >= cutOff)
                    continue;

                await storageService.RemoveFileAsync(storageConfiguration.Bucket, key, cancellationToken);
                deleted++;
            }

            if (deleted > 0)
                logger.LogInformation("Deleted {Deleted} orphaned temp uploads", deleted);

            return deleted;
        }
    }
}
