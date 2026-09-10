using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Tracks.Responses;
using X.PagedList;
using X.PagedList.EF;
using Musify.Application.Contracts;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks
{
    public record GetTracksQuery(string? Name, int PageNumber, int PageSize, string? YoutubeContinuationToken = null)
        : IQuery<ErrorOr<TracksSearchResponse>>;

    public class GetTracksQueryHandler(
        IDatabase database,
        IYouTubeMusicService youTubeMusicService,
        ILogger<GetTracksQueryHandler> logger)
        : IQueryHandler<GetTracksQuery, ErrorOr<TracksSearchResponse>>
    {
        // Mirrors the web client's search-box debounce threshold: a query shorter than this is too
        // likely to still be mid-keystroke to justify a live call to the YouTube Music API for every
        // client that hits this endpoint.
        private const int MinNameLengthForYouTubeSearch = 2;

        public async ValueTask<ErrorOr<TracksSearchResponse>> Handle(GetTracksQuery request, CancellationToken cancellationToken)
        {
            var tracksQuery = database.UserHasTracks
                .AsNoTracking()
                .Include(ut => ((ExternalTrack)ut.Track).TrackArtists)
                    .ThenInclude(trackArtist => trackArtist.Artist)
                .Include(ut => ((LocalTrack)ut.Track).Owner)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Name))
            {
                var normalizedName = request.Name.Trim().ToUpperInvariant();
                tracksQuery = tracksQuery.Where(t => t.Track.NormalizedTitle.Contains(normalizedName));
            }

            var totalCount = await tracksQuery.CountAsync(cancellationToken);

            var pagedEntities = await tracksQuery
                .OrderByDescending(t => t.Track.CreatedAt)
                .ThenBy(t => t.Id)
                .Select(t => new { t.Track, ListensCount = t.Track.ListeningHistories.Count })
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            var localItems = pagedEntities
                .Select(x => TrackSearchItemResponse.FromTrack(TrackApplicationResponse.FromEntity(x.Track, x.ListensCount)))
                .ToList();

            var knownVideoIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in pagedEntities)
            {
                if (entry.Track is ExternalTrack { Source: TrackSource.YouTube } external)
                    knownVideoIds.Add(external.ExternalId);
            }

            var youtubeItems = new List<TrackSearchItemResponse>();
            string? nextContinuationToken = null;
            var youtubeUnavailable = false;

            if (request.Name?.Trim().Length >= MinNameLengthForYouTubeSearch)
                (youtubeItems, nextContinuationToken, youtubeUnavailable) =
                    await SearchYouTubeAsync(request.Name, request.YoutubeContinuationToken, knownVideoIds, cancellationToken);

            return new TracksSearchResponse(
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

        private async Task<(List<TrackSearchItemResponse> Items, string? NextContinuationToken, bool Unavailable)> SearchYouTubeAsync(
            string name,
            string? continuationToken,
            HashSet<string> knownVideoIds,
            CancellationToken cancellationToken)
        {
            var searchResult = await youTubeMusicService.SearchSongsAsync(name, continuationToken ?? string.Empty, cancellationToken);
            if (searchResult.IsError)
            {
                logger.LogInformation(
                    "YouTube search unavailable while combining track results: {Error}", searchResult.FirstError.Description);
                return ([], null, true);
            }

            var items = searchResult.Value.Items
                .Where(song => knownVideoIds.Add(song.VideoId))
                .Select(TrackSearchItemResponse.FromYouTubeSong)
                .ToList();

            var nextToken = string.IsNullOrEmpty(searchResult.Value.ContinuationToken)
                ? null
                : searchResult.Value.ContinuationToken;

            return (items, nextToken, false);
        }
    }
}
