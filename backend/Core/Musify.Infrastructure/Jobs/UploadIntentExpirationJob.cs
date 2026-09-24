using Mediator;
using Musify.Application.UploadIntents;

namespace Musify.Infrastructure.Jobs
{
    public class UploadIntentExpirationJob(IMediator mediator)
    {
        public async Task RunAsync(CancellationToken cancellationToken) =>
            await mediator.Send(new ExpireUploadIntentsCommand(), cancellationToken);
    }
}
