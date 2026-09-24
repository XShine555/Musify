using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;
using MimeMapping;

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
            .Where(t => t.Id == request.TrackId && t.LifeCycleStatus == LifeCycleStatus.Active)
            .Select(t => new { t.Pictures.SmallName, t.Pictures.MediumName, t.Pictures.LargeName })
            .SingleOrDefaultAsync(cancellationToken);

        if (track == null)
            return AppErrors.NotFound("TrackCover", request.TrackId);

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
            return AppErrors.NotFound("TrackCover", request.TrackId);

        var contentType = MimeUtility.GetMimeMapping(name);

        return new TrackCoverLocation(storageConfiguration.Bucket, key, contentType);
    }
}
