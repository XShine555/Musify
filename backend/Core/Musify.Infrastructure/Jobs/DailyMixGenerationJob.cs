using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Mixes;

namespace Musify.Infrastructure.Jobs
{
    public class DailyMixGenerationJob(
        IDatabase database,
        IMediator mediator,
        ILogger<DailyMixGenerationJob> logger)
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var userIds = await database.ListeningHistories
                .AsNoTracking()
                .Where(history => history.IsCounted)
                .Select(history => history.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            logger.LogInformation("Starting daily mix generation for {Count} users", userIds.Count);

            foreach (var userId in userIds)
            {
                var result = await mediator.Send(new GenerateMixesForUserCommand(userId), cancellationToken);
                if (result.IsError)
                {
                    logger.LogWarning("Failed to generate mixes for user {UserId}: {Error}",
                        userId, result.FirstError.Description);
                }
            }

            logger.LogInformation("Finished daily mix generation");
        }
    }
}
