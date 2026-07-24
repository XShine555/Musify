using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Services;

namespace Musify.Application.YouTube
{
    public record GetYouTubeAlbumQuery(string AlbumId)
        : IQuery<ErrorOr<YouTubeAlbumDetail>>;

    public class GetYouTubeAlbumQueryHandler(
        IYouTubeMusicService youTubeMusicService,
        YouTubeTrackProvisioner provisioner,
        ILogger<GetYouTubeAlbumQueryHandler> logger)
        : IQueryHandler<GetYouTubeAlbumQuery, ErrorOr<YouTubeAlbumDetail>>
    {
        public async ValueTask<ErrorOr<YouTubeAlbumDetail>> Handle(GetYouTubeAlbumQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.AlbumId))
                return Error.Validation(description: "The album id must not be empty.");

            var result = await youTubeMusicService.GetAlbumAsync(request.AlbumId, cancellationToken);
            if (result.IsError)
                return result;

            try
            {
                await provisioner.MaterializeAlbumAsync(request.AlbumId, result.Value, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to materialize YouTube album {AlbumId}", request.AlbumId);
            }

            return result;
        }
    }
}
