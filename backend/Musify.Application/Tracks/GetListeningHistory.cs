using Mediator;
using Musify.Application.Contracts;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks
{
    public record GetListeningHistoryQuery(long UserId) : IQuery<IEnumerable<TrackApplicationResponse>>;

    public class GetListeningHistoryQueryHandler(
        IDatabase database)
        : IQueryHandler<GetListeningHistoryQuery, IEnumerable<TrackApplicationResponse>>
    {
        public const int ListSize = 15;

        public ValueTask<IEnumerable<TrackApplicationResponse>> Handle(GetListeningHistoryQuery query, CancellationToken cancellationToken)
        {
            var listeningHistory = database.ListeningHistories
                .Where(l => l.UserId == query.UserId)
                .OrderByDescending(l => l.ListenedAt)
                .Select(t => TrackApplicationResponse.FromEntity(t.Track))
                .Take(ListSize)
                .DistinctBy(t => t.Id)
                .AsEnumerable();

            return ValueTask.FromResult(listeningHistory);
        }
    }
}
