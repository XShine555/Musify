using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Application.Contracts;

namespace Musify.Application.Tracks;

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
        var size = PictureSizeParser.Parse(request.Size);
        var name = size switch
        {
            PictureSize.Small => track.SmallName,
            PictureSize.Large => track.LargeName,
            _ => track.MediumName
        };
        var key = string.IsNullOrEmpty(name) ? null : routes.BuildPicturePath(size, name);

        if (string.IsNullOrEmpty(name) || key == null)
            return Error.NotFound();

        var contentType = name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            ? "image/webp"
            : name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? "image/png"
                : "image/jpeg";

        return new TrackCoverLocation(storageConfiguration.Bucket, key, contentType);
    }
}
