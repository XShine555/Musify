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
            var playList = await database.PlayLists.AsNoTracking()
                .Where(p => p.Id == request.PlayListId)
                .Select(p => new { p.Pictures.SmallName, p.Pictures.MediumName, p.Pictures.LargeName })
                .SingleOrDefaultAsync(cancellationToken);

            if (playList == null)
                return Error.NotFound();

            var routes = playListConfiguration.Routes;
            var (name, key, presetName) = request.Size.ToLowerInvariant() switch
            {
                "small" => (playList.SmallName, BuildKey(routes.BuildSmallPicturePath, playList.SmallName), routes.PresetSmallPicture),
                "large" => (playList.LargeName, BuildKey(routes.BuildLargePicturePath, playList.LargeName), routes.PresetLargePicture),
                _ => (playList.MediumName, BuildKey(routes.BuildMediumPicturePath, playList.MediumName), routes.PresetMediumPicture)
            };

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(key) || name == presetName)
                return Error.NotFound();

            var contentType = MimeUtility.GetMimeMapping(name);
            return new PlayListCoverLocation(storageConfiguration.Bucket, key, contentType);
        }

        private static string? BuildKey(Func<string, string> builder, string? name)
            => string.IsNullOrEmpty(name) ? null : builder(name);
    }
}
