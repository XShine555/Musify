using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    [Table("MixItems")]
    public class MixItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required Guid MixId { get; set; }

        [Required]
        public int Position { get; set; } = 0;

        [Required]
        public required MixItemSource Source { get; set; }

        public Guid? TrackId { get; set; }

        [MaxLength(64)]
        public string? VideoId { get; set; }

        [MaxLength(300)]
        public string? Title { get; set; }

        [MaxLength(300)]
        public string? Artist { get; set; }

        [MaxLength(512)]
        public string? ThumbnailUrl { get; set; }

        [Required]
        public double DurationSeconds { get; set; }

        [Required]
        public bool IsExplicit { get; set; }

        [ForeignKey(nameof(MixId))]
        public Mix Mix { get; set; }

        [ForeignKey(nameof(TrackId))]
        public Track? Track { get; set; }
    }
}
