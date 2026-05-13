using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Tracks.Queries;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Handler
{
    public class GetTrackByIdQueryHandler(IDatabase database)
        : IQueryHandler<GetTrackByIdQuery, Result<TrackApplicationResponse> >
    {
        public async ValueTask<Result<TrackApplicationResponse>> Handle(GetTrackByIdQuery request, CancellationToken cancellationToken)
        {
            var track = await database.Tracks.AsNoTracking()
                .Select(t => TrackApplicationResponse.FromEntity(t))
                .SingleOrDefaultAsync(t => t.Id == request.TrackId, cancellationToken);

            return track ?? Result<TrackApplicationResponse>.NotFound();
        }
    }
}