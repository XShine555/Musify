using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using MimeMapping;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists;

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
        var owned = await database.PlayLists.AsNoTracking()
            .Where(e => e.Id == request.PlayListId && e.LifeCycleStatus == LifeCycleStatus.Active)
            .Select(e => new { OwnerUserId = e.OwnerUserId, e.Pictures })
            .SingleOrDefaultAsync(cancellationToken);

        if (owned?.Pictures == null)
            return AppErrors.NotFound("PlayListCover", request.PlayListId);

        var pictures = owned.Pictures;
        var routes = playListConfiguration.Routes;
        var size = PictureSizeParser.Parse(request.Size);
        var name = size switch
        {
            PictureSize.Small => pictures.SmallName,
            PictureSize.Large => pictures.LargeName,
            _ => pictures.MediumName
        };
        var key = string.IsNullOrEmpty(name) ? null : routes.BuildPicturePath(size, name);

        // The resized files only exist once processing has finished; until then serve the original.
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(key))
        {
            name = pictures.OriginalName;
            key = string.IsNullOrEmpty(name) ? null : routes.BuildOriginalPicturePath(owned.OwnerUserId, name);
        }

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(key))
            return AppErrors.NotFound("PlayListCover", request.PlayListId);

        var contentType = MimeUtility.GetMimeMapping(name);
        return new PlayListCoverLocation(storageConfiguration.Bucket, key, contentType);
    }
}
