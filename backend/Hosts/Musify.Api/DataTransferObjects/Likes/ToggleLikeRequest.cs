using Musify.Domain.ValueObjects;

namespace Musify.Api.DataTransferObjects.Likes;

/// <summary>Toggle a like on a track already in the catalog by <see cref="TrackId"/>, or one from an
/// external source by <see cref="Source"/> + <see cref="ExternalId"/> (provisioning it in the
/// background if needed) — exactly one of the two must be given.</summary>
public record ToggleLikeRequest(Guid? TrackId, TrackSource? Source, string? ExternalId);
