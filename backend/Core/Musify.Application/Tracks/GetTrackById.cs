using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks
{
    public record GetTrackByIdQuery(Guid TrackId)
        : IQuery<ErrorOr<TrackApplicationResponse>>;

    public class GetTrackByIdQueryHandler(IDatabase database)
        : IQueryHandler<GetTrackByIdQuery, ErrorOr<TrackApplicationResponse> >
    {
        public async ValueTask<ErrorOr<TrackApplicationResponse>> Handle(GetTrackByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await database.Tracks.AsNoTracking()
                .Include(t => t.Owner)
                .Include(t => t.Tags)
                .Where(t => t.Id == request.TrackId)
                .Select(t => new { Track = t, ListensCount = t.ListeningHistories.Count })
                .SingleOrDefaultAsync(cancellationToken);

            if (entity is null)
                return Error.NotFound();

            return TrackApplicationResponse.FromEntity(entity.Track, entity.ListensCount);
        }
    }
}
