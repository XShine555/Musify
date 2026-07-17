using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Musify.Domain.Abstractions;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    public class Artist : IAuditable
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(200)]
        public required string NormalizedName { get; set; }

        public long? UserId { get; set; }

        [MaxLength(64)]
        public string? ExternalId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId)) ]
        public User? User { get; set; }

        public ICollection<TrackArtist> TrackArtists { get; set; } = new List<TrackArtist>();
    }
}
