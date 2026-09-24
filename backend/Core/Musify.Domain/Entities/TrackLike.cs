using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
    [Table("TrackLikes")]
    public class TrackLike
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public required long UserId { get; set; }

        public required Guid TrackId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(TrackId))]
        public Track Track { get; set; } = null!;
    }
}
