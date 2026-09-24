using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities;

#pragma warning disable CS8618
public class TrackTag
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required Guid TrackId { get; set; }

    [Required]
    public required Genre Tag { get; set; }

    [ForeignKey(nameof(TrackId))]
    public Track Track { get; set; }
}
