using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Tracks.Queries;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Handler
{
    public class GetTrackByIdQueryHandler(IDatabase database)
        : IQueryHandler<GetTrackByIdQuery, Result<TrackResponse> >
    {
        public async ValueTask<Result<TrackResponse>> Handle(GetTrackByIdQuery request, CancellationToken cancellationToken)
        {
            var track = await database.Tracks.AsNoTracking()
                .Select(t => TrackResponse.FromEntity(t))
                .SingleOrDefaultAsync(t => t.Id == request.TrackId, cancellationToken);

            return track ?? Result<TrackResponse>.NotFound();
        }
    }
}