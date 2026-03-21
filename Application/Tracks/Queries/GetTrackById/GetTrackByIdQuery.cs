using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Tracks.Contracts;

namespace Musify.Application.Tracks.Queries.GetTrackById
{
    public record GetTrackByIdQuery(Guid TrackId)
        : IRequest<GetTrackByIdQuery, Task<Result<TrackResponse> >>;
}