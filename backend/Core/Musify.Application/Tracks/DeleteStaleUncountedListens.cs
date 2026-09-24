using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;

namespace Musify.Application.Tracks;

public record DeleteStaleUncountedListensCommand : ICommand<int>;

public class DeleteStaleUncountedListensCommandHandler(
    IDatabase database,
    ILogger<DeleteStaleUncountedListensCommandHandler> logger)
    : ICommandHandler<DeleteStaleUncountedListensCommand, int>
{
    public async ValueTask<int> Handle(DeleteStaleUncountedListensCommand request, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-ListeningRules.UncountedRetentionDays);

        var stale = await database.ListeningHistories
            .Where(l => !l.IsCounted && l.ListenedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
            return 0;

        database.ListeningHistories.RemoveRange(stale);
        await database.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted {Count} uncounted listens older than {Days} days", stale.Count, ListeningRules.UncountedRetentionDays);
        return stale.Count;
    }
}
