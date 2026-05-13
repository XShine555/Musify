using Ardalis.Result;
using Mediator;
using Musify.Application.Abstractions.Application;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Queries
{
    public record GetTracksByUserIdQuery(
        Guid UserId,
    string? Name = null,
        int PageNumber = 1,
        int PageSize = 10)
        : IQuery<Result<PaginatedResponse<TrackApplicationResponse> > >;
}