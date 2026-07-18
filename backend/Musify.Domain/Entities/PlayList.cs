using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Musify.Domain.Abstractions;
using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    [Table("PlayLists")]
    public class PlayList : IAuditable
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required long UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(50)]
        public required string NormalizedName { get; set; }

        [Required]
        [MaxLength(256)]
        public string? Description { get; set; }

        public PlayListPictures? Pictures { get; set; }

        [Required]
        public LifeCycleStatus LifeCycleStatus { get; set; } = LifeCycleStatus.Active;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId)) ]
        public User User { get; set; }

        public ICollection<PlayListHasTrack> PlayListTracks { get; set; } = new List<PlayListHasTrack>();
    }
}