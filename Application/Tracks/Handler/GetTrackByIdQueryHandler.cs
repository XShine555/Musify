using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Tracks.Queries;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Handler
{
    public class GetTrackByIdQueryHandler(IDatabase database)
        : IRequestHandler<GetTrackByIdQuery, Task<Result<TrackResponse> >>
    {
        public async Task<Result<TrackResponse>> Handle(GetTrackByIdQuery request, CancellationToken cancellationToken)
        {
            var track = await database.Tracks.FindAsync(request.TrackId);

            return track is null
                ? Result.NotFound()
                : Result.Success(TrackResponse.FromEntity(track));
        }
    }
}