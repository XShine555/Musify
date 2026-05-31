using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    [Table("PlayLists")]
    public class PlayList
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
        public required string Description { get; set; } = "No description was provided.";

        [Required]
        [MaxLength(64)]
        public required string OriginalPictureName { get; set; }

        [Required]
        [MaxLength(64)]
        public required string SmallPictureName { get; set; }

        [Required]
        [MaxLength(64)]
        public required string MediumPictureName { get; set; }

        [Required]
        [MaxLength(64)]
        public required string LargePictureName { get; set; }

        [Required]
        public LifeCycleStatus LifeCycleStatus { get; set; } = LifeCycleStatus.Active;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId)) ]
        public User User { get; set; }

        public ICollection<PlayListHasTrack> PlayListTracks { get; set; } = new List<PlayListHasTrack>();
    }
}