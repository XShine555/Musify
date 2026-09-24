using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
    [Table("UserFollows")]
    public class UserFollow
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public required long FollowerId { get; set; }

        public required long FollowedId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(FollowerId))]
        public User Follower { get; set; } = null!;

        [ForeignKey(nameof(FollowedId))]
        public User Followed { get; set; } = null!;
    }
}
