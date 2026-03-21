using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Application;
using Musify.Application.Tracks.Contracts;

namespace Musify.Application.Tracks.Queries.GetTracks
{
    public record GetTracksQuery(int PageNumber = 1, int PageSize = 10)
        : IRequest<GetTracksQuery, Task<Result<PaginatedResponse<TrackResponse> >> >;
}