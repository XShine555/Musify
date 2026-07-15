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
                .Select(p => new { p.SmallPictureName, p.MediumPictureName, p.LargePictureName })
                .SingleOrDefaultAsync(cancellationToken);

            if (playList == null)
                return Error.NotFound();

            var routes = playListConfiguration.Routes;
            var (name, key, presetName) = request.Size.ToLowerInvariant() switch
            {
                "small" => (playList.SmallPictureName, BuildKey(routes.BuildSmallPicturePath, playList.SmallPictureName), routes.PresetSmallPicture),
                "large" => (playList.LargePictureName, BuildKey(routes.BuildLargePicturePath, playList.LargePictureName), routes.PresetLargePicture),
                _ => (playList.MediumPictureName, BuildKey(routes.BuildMediumPicturePath, playList.MediumPictureName), routes.PresetMediumPicture)
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
