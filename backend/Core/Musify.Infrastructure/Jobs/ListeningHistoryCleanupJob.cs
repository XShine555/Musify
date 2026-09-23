using Mediator;
using Musify.Application.Tracks;

namespace Musify.Infrastructure.Jobs
{
    public class ListeningHistoryCleanupJob(IMediator mediator)
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await mediator.Send(new DeleteStaleUncountedListensCommand(), cancellationToken);
        }
    }
}
