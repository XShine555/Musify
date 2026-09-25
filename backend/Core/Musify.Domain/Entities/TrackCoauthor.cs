using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities
{
    [Table("TrackCoauthors")]
    public class TrackCoauthor
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public required Guid TrackId { get; set; }

        public required long UserId { get; set; }

        public CoauthorStatus Status { get; set; } = CoauthorStatus.Pending;

        public DateTime InvitedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }

        [ForeignKey(nameof(TrackId))]
        public Track Track { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
