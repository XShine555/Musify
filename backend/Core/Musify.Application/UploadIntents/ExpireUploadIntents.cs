using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Domain.ValueObjects;

namespace Musify.Application.UploadIntents
{
    /// <summary>Marks overdue upload intents as expired and deletes the ones expired for longer than the retention window.</summary>
    public record ExpireUploadIntentsCommand : ICommand<int>;

    public class ExpireUploadIntentsCommandHandler(
        IDatabase database,
        UploadIntentConfiguration uploadIntentConfiguration,
        ILogger<ExpireUploadIntentsCommandHandler> logger)
        : ICommandHandler<ExpireUploadIntentsCommand, int>
    {
        public async ValueTask<int> Handle(ExpireUploadIntentsCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var expiredIntents = await database.UploadIntents
                .Where(intent => intent.Status == UploadIntentStatus.Issued && intent.ExpiresAt < now)
                .ToListAsync(cancellationToken);

            foreach (var intent in expiredIntents)
                intent.Status = UploadIntentStatus.Expired;

            var retentionCutoff = now.AddDays(-uploadIntentConfiguration.ExpiredIntentsRetentionDays);
            var oldExpiredIntents = await database.UploadIntents
                .Where(intent => intent.Status == UploadIntentStatus.Expired && intent.ExpiresAt < retentionCutoff)
                .ToListAsync(cancellationToken);

            database.UploadIntents.RemoveRange(oldExpiredIntents);

            if (expiredIntents.Count == 0 && oldExpiredIntents.Count == 0)
                return 0;

            await database.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Marked {Expired} upload intents as expired and deleted {Deleted} old ones",
                expiredIntents.Count, oldExpiredIntents.Count);

            return expiredIntents.Count + oldExpiredIntents.Count;
        }
    }
}
