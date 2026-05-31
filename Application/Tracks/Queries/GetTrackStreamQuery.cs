using Ardalis.Result;
using Mediator;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Queries
{
    public record GetTrackStreamQuery(Guid TrackId, long UserId)
        : IQuery<Result<TrackStreamResponse>>;
}
