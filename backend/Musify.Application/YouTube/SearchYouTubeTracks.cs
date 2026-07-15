using ErrorOr;
using Mediator;
using Musify.Application.Contracts;

namespace Musify.Application.YouTube
{
    public record SearchYouTubeTracksQuery(string Query, string ContinuationToken)
        : IQuery<ErrorOr<YouTubeSearchResult>>;

    public class SearchYouTubeTracksQueryHandler(IYouTubeMusicService youTubeMusicService)
        : IQueryHandler<SearchYouTubeTracksQuery, ErrorOr<YouTubeSearchResult>>
    {
        public async ValueTask<ErrorOr<YouTubeSearchResult>> Handle(SearchYouTubeTracksQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Query))
                return Error.Validation(description: "The search query must not be empty.");

            return await youTubeMusicService.SearchSongsAsync(request.Query, request.ContinuationToken, cancellationToken);
        }
    }
}
