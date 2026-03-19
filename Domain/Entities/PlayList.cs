using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    [Table("PlayLists")]
    public class PlayList
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(50)]
        public required string NormalizedName { get; set; }

        [Required]
        [MaxLength(254)]
        public required string Description { get; set; } = "No description was provided.";

        [Required]
        [MaxLength(64)]
        public required string OriginalPictureKeyName { get; set; }

        [Required]
        [MaxLength(64)]
        public required string SmallPictureKeyName { get; set; }

        [Required]
        [MaxLength(64)]
        public required string MediumPictureKeyName { get; set; }

        [Required]
        [MaxLength(64)]
        public required string LargePictureKeyName { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId)) ]
        public User User { get; set; }

        public ICollection<PlayListHasTrack> PlayListTracks { get; set; } = new List<PlayListHasTrack>();
    }
}