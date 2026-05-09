using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
    [Table("User")]
    public class User
    {
        [Key]
        public required Guid Id { get; set; }

        [Required]
        [MaxLength(48)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(48)]
        public required string NormalizedName { get; set; }

        [MaxLength(48)]
        public string? FirstName { get; set; }

        [MaxLength(48)]
        public string? SecondName { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserHasTrack> UserTracks { get; set; } = new List<UserHasTrack>();

        public ICollection<PlayList> PlayLists { get; set; } = new List<PlayList>();
    }
}