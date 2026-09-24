using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
    [Table("ListeningHistory")]
    public class ListeningHistory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public required long UserId { get; set; }

        public required Guid TrackId { get; set; }

        public DateTime ListenedAt { get; set; } = DateTime.UtcNow;

        public double? PlayedSeconds { get; set; }

        public DateTime? LastProgressAt { get; set; }

        public bool IsCounted { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(TrackId))]
        public Track Track { get; set; } = null!;
    }
}
