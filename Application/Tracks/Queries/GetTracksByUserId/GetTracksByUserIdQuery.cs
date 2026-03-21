using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Application;
using Musify.Application.Tracks.Contracts;

namespace Musify.Application.Tracks.Queries.GetTracksByUserId
{
    public record GetTracksByUserIdQuery(
        Guid UserId,
        string Name = "",
        int PageNumber = 1,
        int PageSize = 10)
        : IRequest<GetTracksByUserIdQuery, Task<Result<PaginatedResponse<TrackResponse> > >>;
}