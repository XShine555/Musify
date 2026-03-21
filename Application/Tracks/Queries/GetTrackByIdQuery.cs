using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Queries
{
    public record GetTrackByIdQuery(Guid TrackId)
        : IRequest<GetTrackByIdQuery, Task<Result<TrackResponse> >>;
}