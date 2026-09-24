using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Musify.Domain.Abstractions;
using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities;

[Table("Albums")]
public class Album : IAuditable, IHasLifeCycle, IOwnedEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(200)]
    public required string Title { get; set; }

    [MaxLength(200)]
    public required string NormalizedTitle { get; set; }

    [MaxLength(256)]
    public string? Description { get; set; }

    public int? ReleaseYear { get; set; }

    public EntityPictures? Pictures { get; set; }

    public required long OwnerUserId { get; set; }

    [ForeignKey(nameof(OwnerUserId))]
    public User Owner { get; set; } = null!;

    public LifeCycleStatus LifeCycleStatus { get; set; } = LifeCycleStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AlbumHasTrack> AlbumTracks { get; set; } = new List<AlbumHasTrack>();
}
