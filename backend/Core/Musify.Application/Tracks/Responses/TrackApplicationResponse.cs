using System.Text.Json.Serialization;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks.Responses
{
    // See AlbumApplicationResponse for why [JsonPolymorphic]/[JsonDerivedType] are required
    // (not optional) whenever a base type carries subtype-only data across the wire. Track is a
    // cleaner case than Album: both OwnerUserId (local-only) and ExternalId (external-only) are
    // genuinely exclusive to one side, so the base has no "meaningless sentinel" field at all —
    // every real value is one of the two derived types, hence base is abstract.
    //
    // No explicit Source property here (unlike the domain entities): STJ's polymorphism support
    // already injects a "source" discriminator property with the same "Local"/"YouTube" values a
    // hand-written TrackSource field would have, and a real property with that exact name would
    // collide with it ("contains property 'source' that conflicts with an existing metadata
    // property name"). Use the discriminator (or a type check) instead of a Source field.
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "source")]
    [JsonDerivedType(typeof(LocalTrackApplicationResponse), typeDiscriminator: "Local")]
    [JsonDerivedType(typeof(ExternalTrackApplicationResponse), typeDiscriminator: "YouTube")]
    public abstract record TrackApplicationResponse(
        Guid Id,
        string Title,
        string? Artist,
        ProcessingStatus AudioStatus,
        double Duration,
        int ListensCount,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public static TrackApplicationResponse FromEntity(Track track, int listensCount) =>
            track switch
            {
                LocalTrack local => LocalTrackApplicationResponse.FromEntity(local, listensCount),
                ExternalTrack external => ExternalTrackApplicationResponse.FromEntity(external, listensCount),
                _ => throw new NotSupportedException($"Unsupported track entity type '{track.GetType().Name}'.")
            };

        protected static string? FormatArtist(Track track) => track switch
        {
            LocalTrack local => local.Owner.Name,
            ExternalTrack { TrackArtists.Count: > 0 } external =>
                string.Join(", ", external.TrackArtists
                    .OrderBy(trackArtist => trackArtist.Position)
                    .Select(trackArtist => trackArtist.Artist.Name)),
            _ => null
        };
    }
}
