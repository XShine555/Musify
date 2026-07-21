using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Musify.Domain.Abstractions;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    [Table("Mixes")]
    public class Mix : IAuditable
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required long UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Title { get; set; }

        [MaxLength(256)]
        public string? Subtitle { get; set; }

        [Required]
        public int Position { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        public ICollection<MixItem> Items { get; set; } = new List<MixItem>();
    }
}
