using ErrorOr;
using Mediator;
using Musify.Application.Contracts;

namespace Musify.Application.YouTube
{
    public record GetYouTubeAlbumQuery(string AlbumId)
        : IQuery<ErrorOr<YouTubeAlbumDetail>>;

    public class GetYouTubeAlbumQueryHandler(IYouTubeMusicService youTubeMusicService)
        : IQueryHandler<GetYouTubeAlbumQuery, ErrorOr<YouTubeAlbumDetail>>
    {
        public async ValueTask<ErrorOr<YouTubeAlbumDetail>> Handle(GetYouTubeAlbumQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.AlbumId))
                return Error.Validation(description: "The album id must not be empty.");

            return await youTubeMusicService.GetAlbumAsync(request.AlbumId, cancellationToken);
        }
    }
}
