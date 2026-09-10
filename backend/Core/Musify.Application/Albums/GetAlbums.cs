using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using X.PagedList;
using X.PagedList.EF;

namespace Musify.Application.Albums
{
    public record GetAlbumsQuery(string? Title, int PageNumber, int PageSize, string? YoutubeContinuationToken = null)
        : IQuery<ErrorOr<AlbumsSearchResponse>>;

    public class GetAlbumsQueryHandler(
        IDatabase database,
        IYouTubeMusicService youTubeMusicService,
        ILogger<GetAlbumsQueryHandler> logger)
        : IQueryHandler<GetAlbumsQuery, ErrorOr<AlbumsSearchResponse>>
    {
        // Mirrors GetTracksQueryHandler's threshold: a title shorter than this is too likely to
        // still be mid-keystroke to justify a live call to the external search API.
        private const int MinTitleLengthForYouTubeSearch = 2;

        public async ValueTask<ErrorOr<AlbumsSearchResponse>> Handle(GetAlbumsQuery request, CancellationToken cancellationToken)
        {
            var albumsQuery = database.Albums
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Title))
            {
                var normalizedTitle = request.Title.Trim().ToUpperInvariant();
                albumsQuery = albumsQuery.Where(album => album.NormalizedTitle.Contains(normalizedTitle));
            }

            var totalCount = await albumsQuery.CountAsync(cancellationToken);

            var pagedEntities = await albumsQuery
                .OrderByDescending(album => album.CreatedAt)
                .Select(album => new
                {
                    Album = album,
                    TrackCount = album.AlbumTracks.Count,
                    CoverTrackIds = album.AlbumTracks
                        .OrderBy(albumTrack => albumTrack.TrackNumber)
                        .Take(AlbumApplicationResponse.CoverTrackCount)
                        .Select(albumTrack => albumTrack.TrackId)
                        .ToList()
                })
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            var localItems = pagedEntities
                .Select(entry => AlbumSearchItemResponse.FromAlbum(
                    AlbumApplicationResponse.FromEntity(entry.Album, entry.TrackCount, entry.CoverTrackIds)))
                .ToList();

            var knownExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in pagedEntities)
            {
                if (entry.Album is ExternalAlbum { Source: TrackSource.YouTube } external)
                    knownExternalIds.Add(external.ExternalId);
            }

            var youtubeItems = new List<AlbumSearchItemResponse>();
            string? nextContinuationToken = null;
            var youtubeUnavailable = false;

            if (request.Title?.Trim().Length >= MinTitleLengthForYouTubeSearch)
                (youtubeItems, nextContinuationToken, youtubeUnavailable) =
                    await SearchYouTubeAsync(request.Title, request.YoutubeContinuationToken, knownExternalIds, cancellationToken);

            return AlbumsSearchResponse.Create(
                [.. localItems, .. youtubeItems],
                pagedEntities.PageNumber,
                pagedEntities.PageSize,
                pagedEntities.PageCount,
                pagedEntities.TotalItemCount,
                pagedEntities.HasPreviousPage,
                pagedEntities.HasNextPage,
                nextContinuationToken,
                youtubeUnavailable);
        }

        private async Task<(List<AlbumSearchItemResponse> Items, string? NextContinuationToken, bool Unavailable)> SearchYouTubeAsync(
            string title,
            string? continuationToken,
            HashSet<string> knownExternalIds,
            CancellationToken cancellationToken)
        {
            var searchResult = await youTubeMusicService.SearchAlbumsAsync(title, continuationToken ?? string.Empty, cancellationToken);
            if (searchResult.IsError)
            {
                logger.LogInformation(
                    "YouTube album search unavailable while combining album results: {Error}", searchResult.FirstError.Description);
                return ([], null, true);
            }

            var items = searchResult.Value.Items
                .Where(album => knownExternalIds.Add(album.AlbumId))
                .Select(AlbumSearchItemResponse.FromYouTubeAlbum)
                .ToList();

            var nextToken = string.IsNullOrEmpty(searchResult.Value.ContinuationToken)
                ? null
                : searchResult.Value.ContinuationToken;

            return (items, nextToken, false);
        }
    }
}
