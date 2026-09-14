using System.Text.Json.Serialization;
using Musify.Domain.Entities;
using Musify.Application.Serialization;

namespace Musify.Application.Albums.Responses
{
    // [JsonPolymorphic]/[JsonDerivedType] are required here, not optional: without them
    // System.Text.Json serializes strictly by the declared CLR type at each call site
    // (AlbumApplicationResponse, both as a bare return type and nested inside
    // AlbumSearchItemResponse.Album), so an ExternalAlbumApplicationResponse instance would
    // silently lose ExternalId/ThumbnailUrl on the wire. This also gives every response a
    // "source" discriminator field for free, matching AlbumSearchItemResponse.Source.
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "source")]
    [JsonDerivedType(typeof(AlbumApplicationResponse), typeDiscriminator: "Local")]
    [JsonDerivedType(typeof(ExternalAlbumApplicationResponse), typeDiscriminator: "YouTube")]
    public record AlbumApplicationResponse(
        Guid Id,
        string Title,
        string? Description,
        int? ReleaseYear,
        [property: JsonConverter(typeof(LongAsStringConverter))] long OwnerUserId,
        int TrackCount,
        string? SmallImageKeyName,
        string? MediumImageKeyName,
        string? LargeImageKeyName,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IReadOnlyList<Guid> CoverTrackIds)
    {
        public const int CoverTrackCount = 4;

        public static AlbumApplicationResponse FromEntity(
            UserAlbum album,
            int trackCount,
            IReadOnlyList<Guid>? coverTrackIds = null)
        {
            return new AlbumApplicationResponse(
                album.Id,
                album.Title,
                album.Description,
                album.ReleaseYear,
                album.OwnerUserId,
                trackCount,
                album.Pictures?.SmallName,
                album.Pictures?.MediumName,
                album.Pictures?.LargeName,
                album.CreatedAt,
                album.UpdatedAt,
                coverTrackIds ?? []);
        }

        public static AlbumApplicationResponse FromEntity(
            Album album,
            int trackCount,
            IReadOnlyList<Guid>? coverTrackIds = null)
        {
            return album switch
            {
                ExternalAlbum externalAlbum => ExternalAlbumApplicationResponse.FromEntity(externalAlbum, trackCount, coverTrackIds),
                UserAlbum userAlbum => FromEntity(userAlbum, trackCount, coverTrackIds),
                _ => throw new NotSupportedException($"Unsupported album entity type '{album.GetType().Name}'.")
            };
        }
    }
}
