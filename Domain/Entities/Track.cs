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
        [MaxLength(64)]
        public required string OriginalAudioName { get; set; }

        [MaxLength(64)]
        public string AudioFolderName { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<UserHasTrack> UserTracks { get; set; } = new List<UserHasTrack>();

        public ICollection<PlayListHasTrack> PlayListTracks { get; set; } = new List<PlayListHasTrack>();
    }
}