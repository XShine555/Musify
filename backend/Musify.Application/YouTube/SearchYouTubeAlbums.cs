using ErrorOr;
using Mediator;
using Musify.Application.Contracts;

namespace Musify.Application.YouTube
{
    public record SearchYouTubeAlbumsQuery(string Query, string ContinuationToken)
        : IQuery<ErrorOr<YouTubeAlbumSearchResult>>;

    public class SearchYouTubeAlbumsQueryHandler(IYouTubeMusicService youTubeMusicService)
        : IQueryHandler<SearchYouTubeAlbumsQuery, ErrorOr<YouTubeAlbumSearchResult>>
    {
        public async ValueTask<ErrorOr<YouTubeAlbumSearchResult>> Handle(SearchYouTubeAlbumsQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Query))
                return Error.Validation(description: "The search query must not be empty.");

            return await youTubeMusicService.SearchAlbumsAsync(request.Query, request.ContinuationToken, cancellationToken);
        }
    }
}
