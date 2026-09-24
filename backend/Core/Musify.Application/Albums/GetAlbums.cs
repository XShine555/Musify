using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Application.Shared;

namespace Musify.Application.Albums
{
    public record GetAlbumsQuery(string? Title, int PageNumber, int PageSize)
        : IQuery<PaginatedResponse<AlbumApplicationResponse>>;

    public class GetAlbumsQueryHandler(IDatabase database)
        : IQueryHandler<GetAlbumsQuery, PaginatedResponse<AlbumApplicationResponse>>
    {
        public async ValueTask<PaginatedResponse<AlbumApplicationResponse>> Handle(GetAlbumsQuery request, CancellationToken cancellationToken)
        {
            var albumsQuery = database.Albums
                .AsNoTracking()
                .Active();

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                var normalizedTitle = TextNormalizer.Normalize(request.Title);
                albumsQuery = albumsQuery.Where(album => album.NormalizedTitle.Contains(normalizedTitle));
            }

            return await albumsQuery
                .OrderByDescending(album => album.CreatedAt)
                .ThenBy(album => album.Id)
                .SelectResponse()
                .ToPaginatedAsync(new PageRequest(request.PageNumber, request.PageSize), cancellationToken);
        }
    }
}
