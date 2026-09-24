using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using MimeMapping;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Pictures
{
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
                CoverOwner.Track => await FindTrackSourceAsync(request.Id, cancellationToken),
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

            var (name, key) = !string.IsNullOrEmpty(resizedName)
                ? (resizedName, routes.BuildPicturePath(request.Size, resizedName))
                : !string.IsNullOrEmpty(source.Original)
                    ? (source.Original, routes.BuildOriginalPicturePath(source.OwnerUserId, source.Original))
                    : (null, null);

            if (name == null || key == null)
                return AppErrors.NotFound($"{entity}Cover", request.Id);

            return new CoverLocation(storageConfiguration.Bucket, key, MimeUtility.GetMimeMapping(name));
        }

        private async Task<(CoverSource? Source, PictureRoutes Routes)> FindTrackSourceAsync(Guid trackId, CancellationToken cancellationToken)
        {
            var track = await database.Tracks.AsNoTracking()
                .Where(t => t.Id == trackId && t.LifeCycleStatus == LifeCycleStatus.Active)
                .Select(t => new
                {
                    Own = new CoverSource(t.OwnerUserId, t.Pictures.OriginalName, t.Pictures.SmallName, t.Pictures.MediumName, t.Pictures.LargeName),
                    Album = t.AlbumTracks
                        .Where(albumTrack => albumTrack.Album.LifeCycleStatus == LifeCycleStatus.Active && albumTrack.Album.Pictures != null)
                        .OrderBy(albumTrack => albumTrack.AddedAt)
                        .ThenBy(albumTrack => albumTrack.AlbumId)
                        .Select(albumTrack => new CoverSource(
                            albumTrack.Album.OwnerUserId,
                            albumTrack.Album.Pictures!.OriginalName,
                            albumTrack.Album.Pictures.SmallName,
                            albumTrack.Album.Pictures.MediumName,
                            albumTrack.Album.Pictures.LargeName))
                        .FirstOrDefault()
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (track == null)
                return (null, trackConfiguration.Routes);

            return string.IsNullOrEmpty(track.Own.Original) && track.Album != null
                ? (track.Album, albumConfiguration.Routes)
                : (track.Own, trackConfiguration.Routes);
        }
    }
}
