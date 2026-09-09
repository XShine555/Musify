using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using MimeMapping;
using Musify.Application.Configuration;
using Musify.Application.Contracts;

namespace Musify.Application.PlayLists
{
    public record GetPlayListCoverQuery(Guid PlayListId, string Size)
        : IQuery<ErrorOr<PlayListCoverLocation>>;

    public record PlayListCoverLocation(string Bucket, string Key, string ContentType);

    public class GetPlayListCoverQueryHandler(
        IDatabase database,
        ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration)
        : IQueryHandler<GetPlayListCoverQuery, ErrorOr<PlayListCoverLocation>>
    {
        public async ValueTask<ErrorOr<PlayListCoverLocation>> Handle(GetPlayListCoverQuery request, CancellationToken cancellationToken)
        {
            var pictures = await database.PlayLists.AsNoTracking()
                .Where(p => p.Id == request.PlayListId)
                .Select(p => p.Pictures)
                .SingleOrDefaultAsync(cancellationToken);

            if (pictures is null)
                return Error.NotFound();

            var routes = playListConfiguration.Routes;
            var (name, key) = request.Size.ToLowerInvariant() switch
            {
                "small" => (pictures.SmallName, BuildKey(routes.BuildSmallPicturePath, pictures.SmallName)),
                "large" => (pictures.LargeName, BuildKey(routes.BuildLargePicturePath, pictures.LargeName)),
                _ => (pictures.MediumName, BuildKey(routes.BuildMediumPicturePath, pictures.MediumName))
            };

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(key))
                return Error.NotFound();

            var contentType = MimeUtility.GetMimeMapping(name);
            return new PlayListCoverLocation(storageConfiguration.Bucket, key, contentType);
        }

        private static string? BuildKey(Func<string, string> builder, string? name)
            => string.IsNullOrEmpty(name) ? null : builder(name);
    }
}
