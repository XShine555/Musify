using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    [Table("ListeningHistory")]
    public class ListeningHistory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required long UserId { get; set; }

        [Required]
        public required Guid TrackId { get; set; }

        [Required]
        public DateTime ListenedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId) )]
        public User User { get; set; }

        [ForeignKey(nameof(TrackId)) ]
        public Track Track { get; set; }
    }
}