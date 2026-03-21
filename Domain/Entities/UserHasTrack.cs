using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    [Table("UserHasTrack")]
    public class UserHasTrack
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required Guid UserId { get; set; }

        [Required]
        public required Guid TrackId { get; set; }

        [ForeignKey(nameof(UserId)) ]
        public User User { get; set; }

        [ForeignKey(nameof(TrackId)) ]
        public Track Track { get; set; }
    }
}