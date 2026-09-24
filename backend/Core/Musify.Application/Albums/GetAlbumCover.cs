using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using MimeMapping;
using Musify.Application.Configuration;
using Musify.Application.Contracts;

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
        var pictures = await database.Albums.AsNoTracking()
            .Where(a => a.Id == request.AlbumId)
            .Select(a => a.Pictures)
            .SingleOrDefaultAsync(cancellationToken);

        if (pictures == null)
            return Error.NotFound();

        var routes = albumConfiguration.Routes;
        var (name, key) = request.Size.ToLowerInvariant() switch
        {
            "small" => (pictures.SmallName, BuildKey(routes.BuildSmallPicturePath, pictures.SmallName)),
            "large" => (pictures.LargeName, BuildKey(routes.BuildLargePicturePath, pictures.LargeName)),
            _ => (pictures.MediumName, BuildKey(routes.BuildMediumPicturePath, pictures.MediumName))
        };

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(key))
            return Error.NotFound();

        var contentType = MimeUtility.GetMimeMapping(name);
        return new AlbumCoverLocation(storageConfiguration.Bucket, key, contentType);
    }

    private static string? BuildKey(Func<string, string> builder, string? name)
        => string.IsNullOrEmpty(name) ? null : builder(name);
}
