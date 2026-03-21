using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Application;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Queries
{
    public record GetTracksQuery(int PageNumber = 1, int PageSize = 10)
        : IRequest<GetTracksQuery, Task<Result<PaginatedResponse<TrackResponse> >> >;
}