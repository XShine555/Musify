using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using MimeMapping;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Albums;

public record GetAlbumCoverQuery(Guid AlbumId, string Size)
    : IQuery<ErrorOr<AlbumCoverLocation>>;

public record AlbumCoverLocation(string Bucket, string Key, string ContentType);

public class GetAlbumCoverQueryHandler(
    IDatabase database,
    ApplicationStorageConfiguration storageConfiguration,
    AlbumConfiguration albumConfiguration)
    : IQueryHandler<GetAlbumCoverQuery, ErrorOr<AlbumCoverLocation>>
{
    public async ValueTask<ErrorOr<AlbumCoverLocation>> Handle(GetAlbumCoverQuery request, CancellationToken cancellationToken)
    {
        var owned = await database.Albums.AsNoTracking()
            .Where(e => e.Id == request.AlbumId && e.LifeCycleStatus == LifeCycleStatus.Active)
            .Select(e => new { OwnerUserId = e.OwnerUserId, e.Pictures })
            .SingleOrDefaultAsync(cancellationToken);

        if (owned?.Pictures == null)
            return AppErrors.NotFound("AlbumCover", request.AlbumId);

        var pictures = owned.Pictures;
        var routes = albumConfiguration.Routes;
        var (name, key) = request.Size.ToLowerInvariant() switch
        {
            "small" => (pictures.SmallName, BuildKey(routes.BuildSmallPicturePath, pictures.SmallName)),
            "large" => (pictures.LargeName, BuildKey(routes.BuildLargePicturePath, pictures.LargeName)),
            _ => (pictures.MediumName, BuildKey(routes.BuildMediumPicturePath, pictures.MediumName))
        };

        // The resized files only exist once processing has finished; until then serve the original.
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(key))
        {
            name = pictures.OriginalName;
            key = string.IsNullOrEmpty(name) ? null : routes.BuildOriginalPicturePath(owned.OwnerUserId, name);
        }

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(key))
            return AppErrors.NotFound("AlbumCover", request.AlbumId);

        var contentType = MimeUtility.GetMimeMapping(name);
        return new AlbumCoverLocation(storageConfiguration.Bucket, key, contentType);
    }

    private static string? BuildKey(Func<string, string> builder, string? name)
        => string.IsNullOrEmpty(name) ? null : builder(name);
}
