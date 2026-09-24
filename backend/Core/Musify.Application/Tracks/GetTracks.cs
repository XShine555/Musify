using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks
{
    public record GetTracksQuery(string? Name, int PageNumber, int PageSize, Genre? Genre = null)
        : IQuery<PaginatedResponse<TrackApplicationResponse>>;

    public class GetTracksQueryHandler(IDatabase database)
        : IQueryHandler<GetTracksQuery, PaginatedResponse<TrackApplicationResponse>>
    {
        public async ValueTask<PaginatedResponse<TrackApplicationResponse>> Handle(GetTracksQuery request, CancellationToken cancellationToken)
        {
            var tracksQuery = database.Tracks
                .AsNoTracking()
                .Active();

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var normalizedName = TextNormalizer.Normalize(request.Name);
                tracksQuery = tracksQuery.Where(track => track.NormalizedTitle.Contains(normalizedName));
            }

            if (request.Genre is { } genre)
                tracksQuery = tracksQuery.Where(track => track.Tags.Any(tag => tag.Tag == genre));

            return await tracksQuery
                .OrderByDescending(track => track.CreatedAt)
                .ThenBy(track => track.Id)
                .SelectResponse()
                .ToPaginatedAsync(new PageRequest(request.PageNumber, request.PageSize), cancellationToken);
        }
    }
}
