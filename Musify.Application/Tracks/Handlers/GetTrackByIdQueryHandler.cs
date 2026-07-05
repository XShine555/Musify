using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Tracks.Queries;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Handlers
{
    public class GetTrackByIdQueryHandler(IDatabase database)
        : IQueryHandler<GetTrackByIdQuery, Result<TrackApplicationResponse> >
    {
        public async ValueTask<Result<TrackApplicationResponse>> Handle(GetTrackByIdQuery request, CancellationToken cancellationToken)
        {
            var track = await database.Tracks.AsNoTracking()
                .Where(t => t.Id == request.TrackId)
                .Select(t => TrackApplicationResponse.FromEntity(t))
                .SingleOrDefaultAsync(cancellationToken);

            return track is null
                ? Result<TrackApplicationResponse>.NotFound()
                : Result.Success(track);
        }
    }
}