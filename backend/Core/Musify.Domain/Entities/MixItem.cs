using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    [Table("MixItems")]
    public class MixItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required Guid MixId { get; set; }

        [Required]
        public int Position { get; set; } = 0;

        [Required]
        public required Guid TrackId { get; set; }

        [ForeignKey(nameof(MixId))]
        public Mix Mix { get; set; }

        [ForeignKey(nameof(TrackId))]
        public Track Track { get; set; }
    }
}
