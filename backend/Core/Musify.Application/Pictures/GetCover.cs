using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using MimeMapping;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Pictures;

public enum CoverOwner
{
    Track,
    Album,
    PlayList
}

public record GetCoverQuery(CoverOwner Owner, Guid Id, PictureSize Size = PictureSize.Medium)
    : IQuery<ErrorOr<CoverLocation>>;

public record CoverLocation(string Bucket, string Key, string ContentType);

public class GetCoverQueryHandler(
    IDatabase database,
    ApplicationStorageConfiguration storageConfiguration,
    TrackConfiguration trackConfiguration,
    AlbumConfiguration albumConfiguration,
    PlayListConfiguration playListConfiguration)
    : IQueryHandler<GetCoverQuery, ErrorOr<CoverLocation>>
{
    private sealed record CoverSource(long OwnerUserId, string? Original, string? Small, string? Medium, string? Large);

    public async ValueTask<ErrorOr<CoverLocation>> Handle(GetCoverQuery request, CancellationToken cancellationToken)
    {
        var (source, routes) = request.Owner switch
        {
            CoverOwner.Track => (await database.Tracks.AsNoTracking()
                .Where(t => t.Id == request.Id && t.LifeCycleStatus == LifeCycleStatus.Active)
                .Select(t => new CoverSource(t.OwnerUserId, t.Pictures.OriginalName, t.Pictures.SmallName, t.Pictures.MediumName, t.Pictures.LargeName))
                .SingleOrDefaultAsync(cancellationToken), trackConfiguration.Routes),
            CoverOwner.Album => (await database.Albums.AsNoTracking()
                .Where(a => a.Id == request.Id && a.LifeCycleStatus == LifeCycleStatus.Active && a.Pictures != null)
                .Select(a => new CoverSource(a.OwnerUserId, a.Pictures!.OriginalName, a.Pictures.SmallName, a.Pictures.MediumName, a.Pictures.LargeName))
                .SingleOrDefaultAsync(cancellationToken), albumConfiguration.Routes),
            _ => (await database.PlayLists.AsNoTracking()
                .Where(p => p.Id == request.Id && p.LifeCycleStatus == LifeCycleStatus.Active && p.Pictures != null)
                .Select(p => new CoverSource(p.OwnerUserId, p.Pictures!.OriginalName, p.Pictures.SmallName, p.Pictures.MediumName, p.Pictures.LargeName))
                .SingleOrDefaultAsync(cancellationToken), playListConfiguration.Routes)
        };

        var entity = request.Owner.ToString();
        if (source == null)
            return AppErrors.NotFound($"{entity}Cover", request.Id);

        var resizedName = request.Size switch
        {
            PictureSize.Small => source.Small,
            PictureSize.Large => source.Large,
            _ => source.Medium
        };

        // The resized files only exist once processing has finished; until then serve the original.
        var (name, key) = !string.IsNullOrEmpty(resizedName)
            ? (resizedName, routes.BuildPicturePath(request.Size, resizedName))
            : !string.IsNullOrEmpty(source.Original)
                ? (source.Original, routes.BuildOriginalPicturePath(source.OwnerUserId, source.Original))
                : (null, null);

        if (name == null || key == null)
            return AppErrors.NotFound($"{entity}Cover", request.Id);

        return new CoverLocation(storageConfiguration.Bucket, key, MimeUtility.GetMimeMapping(name));
    }
}
