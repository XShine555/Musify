using Musify.Application.Contracts;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Albums.Responses
{
    /// <summary>One item of a combined local/external album listing. Exactly one of
    /// <see cref="Album"/> or <see cref="YouTubeAlbum"/> is populated: <see cref="Album"/> for
    /// anything already in the catalog (locally created albums and external albums materialized
    /// earlier), <see cref="YouTubeAlbum"/> for a live external search hit that hasn't been
    /// materialized yet, so it has no album id.</summary>
    public record AlbumSearchItemResponse(
        TrackSource Source,
        AlbumApplicationResponse? Album,
        YouTubeAlbumResult? YouTubeAlbum)
    {
        public static AlbumSearchItemResponse FromAlbum(AlbumApplicationResponse album) =>
            new(album is ExternalAlbumApplicationResponse ? TrackSource.YouTube : TrackSource.Local, album, null);

        public static AlbumSearchItemResponse FromYouTubeAlbum(YouTubeAlbumResult album) =>
            new(TrackSource.YouTube, null, album);
    }
}
