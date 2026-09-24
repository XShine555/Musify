using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities;

[Table("MixItems")]
public class MixItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid MixId { get; set; }

    public int Position { get; set; }

    public required Guid TrackId { get; set; }

    [ForeignKey(nameof(MixId))]
    public Mix Mix { get; set; } = null!;

    [ForeignKey(nameof(TrackId))]
    public Track Track { get; set; } = null!;
}
