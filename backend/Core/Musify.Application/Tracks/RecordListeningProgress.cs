using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;

namespace Musify.Application.Tracks
{
    public record RecordListeningProgressCommand(
        long UserId,
        Guid ListenId,
        double PlayedSeconds)
        : ICommand<ErrorOr<Success>>;

    public class RecordListeningProgressCommandHandler(IDatabase database)
        : ICommandHandler<RecordListeningProgressCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(RecordListeningProgressCommand request, CancellationToken cancellationToken)
        {
            var listen = await database.ListeningHistories
                .Include(l => l.Track)
                .SingleOrDefaultAsync(l => l.Id == request.ListenId && l.UserId == request.UserId, cancellationToken);

            if (listen == null)
                return AppErrors.NotFound("Listen", request.ListenId);

            var now = DateTime.UtcNow;
            var durationCap = listen.Track.DurationSeconds + ListeningRules.DurationToleranceSeconds;
            var clockCap = (now - listen.ListenedAt).TotalSeconds + ListeningRules.ClockToleranceSeconds;
            var accepted = Math.Min(request.PlayedSeconds, Math.Min(durationCap, clockCap));

            listen.PlayedSeconds = Math.Max(listen.PlayedSeconds ?? 0, accepted);
            listen.LastProgressAt = now;
            listen.IsCounted = listen.IsCounted
                || listen.PlayedSeconds >= ListeningRules.CountedThreshold(listen.Track.DurationSeconds);

            await database.SaveChangesAsync(cancellationToken);

            return Result.Success;
        }
    }
}
