using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Musify.Domain.Abstractions;
using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities;

#pragma warning disable CS8618
public class Album : IAuditable
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [Required]
    [MaxLength(200)]
    public required string NormalizedTitle { get; set; }

    [MaxLength(256)]
    public string? Description { get; set; }

    public int? ReleaseYear { get; set; }

    public AlbumPictures? Pictures { get; set; }

    [Required]
    public required long OwnerUserId { get; set; }

    [ForeignKey(nameof(OwnerUserId))]
    public User Owner { get; set; }

    [Required]
    public LifeCycleStatus LifeCycleStatus { get; set; } = LifeCycleStatus.Active;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AlbumHasTrack> AlbumTracks { get; set; } = new List<AlbumHasTrack>();
}
