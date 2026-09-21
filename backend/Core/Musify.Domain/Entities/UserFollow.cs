using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    [Table("UserFollows")]
    public class UserFollow
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required long FollowerId { get; set; }

        [Required]
        public required long FollowedId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(FollowerId))]
        public User Follower { get; set; }

        [ForeignKey(nameof(FollowedId))]
        public User Followed { get; set; }
    }
}
