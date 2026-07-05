using Ardalis.Result;
using Mediator;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Queries
{
    public record GetTrackByIdQuery(Guid TrackId)
        : IQuery<Result<TrackApplicationResponse>>;
}