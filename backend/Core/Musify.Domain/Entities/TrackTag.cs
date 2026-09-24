using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities
{
    [Table("TrackTags")]
    public class TrackTag
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public required Guid TrackId { get; set; }

        public required Genre Tag { get; set; }

        [ForeignKey(nameof(TrackId))]
        public Track Track { get; set; } = null!;
    }
}
