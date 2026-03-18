using System.ComponentModel.DataAnnotations;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    public class Track
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public required string Title { get; set; }

        [Required]
        [MaxLength(50)]
        public required string NormalizedTitle { get; set; }

        [Required]
        [MaxLength(64)]
        public required string ImageName { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<User> Users { get; set; } = new List<User>();

        public ICollection<PlayListHasTrack> PlayListTracks { get; set; } = new List<PlayListHasTrack>();
    }
}