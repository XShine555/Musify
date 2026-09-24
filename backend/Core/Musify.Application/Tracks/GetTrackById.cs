using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks;

public record GetTrackByIdQuery(Guid TrackId)
    : IQuery<ErrorOr<TrackApplicationResponse>>;

public class GetTrackByIdQueryHandler(IDatabase database)
    : IQueryHandler<GetTrackByIdQuery, ErrorOr<TrackApplicationResponse>>
{
    public async ValueTask<ErrorOr<TrackApplicationResponse>> Handle(GetTrackByIdQuery request, CancellationToken cancellationToken)
    {
        var track = await database.Tracks
            .AsNoTracking()
            .Active()
            .Where(t => t.Id == request.TrackId)
            .SelectResponse()
            .SingleOrDefaultAsync(cancellationToken);

        return track == null
            ? AppErrors.NotFound("Track", request.TrackId)
            : track;
    }
}
