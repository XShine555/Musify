using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Application.Contracts;

namespace Musify.Application.Tracks
{
    public record GetTrackCoverQuery(Guid TrackId, string Size)
        : IQuery<ErrorOr<TrackCoverLocation>>;

    public record TrackCoverLocation(string Bucket, string Key, string ContentType);

    public class GetTrackCoverQueryHandler(
        IDatabase database,
        ApplicationStorageConfiguration storageConfiguration,
        TrackConfiguration trackConfiguration)
        : IQueryHandler<GetTrackCoverQuery, ErrorOr<TrackCoverLocation>>
    {
        public async ValueTask<ErrorOr<TrackCoverLocation>> Handle(GetTrackCoverQuery request, CancellationToken cancellationToken)
        {
            var track = await database.Tracks.AsNoTracking()
                .Where(t => t.Id == request.TrackId)
                .Select(t => new { t.Pictures.SmallName, t.Pictures.MediumName, t.Pictures.LargeName })
                .SingleOrDefaultAsync(cancellationToken);

            if (track == null)
                return Error.NotFound();

            var routes = trackConfiguration.Routes;
            var (name, key) = request.Size.ToLowerInvariant() switch
            {
                "small" => (track.SmallName, BuildKey(routes.BuildSmallPicturePath, track.SmallName)),
                "large" => (track.LargeName, BuildKey(routes.BuildLargePicturePath, track.LargeName)),
                _ => (track.MediumName, BuildKey(routes.BuildMediumPicturePath, track.MediumName))
            };

            if (string.IsNullOrEmpty(name) || key == null)
                return Error.NotFound();

            var contentType = name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
                ? "image/webp"
                : name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    ? "image/png"
                    : "image/jpeg";

            return new TrackCoverLocation(storageConfiguration.Bucket, key, contentType);
        }

        private static string? BuildKey(Func<string, string> builder, string? name)
            => string.IsNullOrEmpty(name)? null: builder(name);
    }
}