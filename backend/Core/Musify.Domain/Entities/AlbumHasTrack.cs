using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities;

#pragma warning disable CS8618
[Table("AlbumHasTrack")]
public class AlbumHasTrack
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required Guid AlbumId { get; set; }

    [Required]
    public required Guid TrackId { get; set; }

    [Required]
    public int TrackNumber { get; set; } = 1;

    [Required]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(AlbumId))]
    public Album Album { get; set; }

    [ForeignKey(nameof(TrackId))]
    public Track Track { get; set; }
}
