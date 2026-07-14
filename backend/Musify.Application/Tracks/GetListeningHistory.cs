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
        public ValueTask<IEnumerable<TrackApplicationResponse>> Handle(GetListeningHistoryQuery query, CancellationToken cancellationToken)
        {
            var listeningHistory = database.ListeningHistories
                .Where(l => l.UserId == query.UserId)
                .Select(l => l.TrackId)
                .ToList();

            var tracks = database.Tracks
                .Where(t => listeningHistory.Contains(t.Id))
                .Select(TrackApplicationResponse.FromEntity)
                .AsEnumerable();

            return ValueTask.FromResult(tracks);
        }
    }
}
