using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

using Musify.Domain.ValueObjects;

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

        [MaxLength(64)]
        public string? SmallPictureName { get; set; }

        [MaxLength(64)]
        public string? MediumPictureName { get; set; }

        [MaxLength(64)]
        public string? LargePictureName { get; set; }

        [Required]
        [MaxLength(64)]
        public required string OriginalAudioName { get; set; }

        [MaxLength(64)]
        public string? AudioFolderName { get; set; }

        [Required]
        public ProcessingStatus PicturesProcessingStatus { get; set; } = ProcessingStatus.Pending;

        [Required]
        public ProcessingStatus AudioTranscodeProcessingStatus { get; set; } = ProcessingStatus.Pending;

        [Required]
        public LifeCycleStatus LifeCycleStatus { get; set; } = LifeCycleStatus.Active;

        [Required]
        public int RetryCount { get; set; } = 0;

        public DateTime? LastRetryAt { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<UserHasTrack> UserTracks { get; set; } = new List<UserHasTrack>();

        public ICollection<PlayListHasTrack> PlayListTracks { get; set; } = new List<PlayListHasTrack>();

        [NotMapped]
        [MemberNotNullWhen(true, nameof(SmallPictureName)) ]
        [MemberNotNullWhen(true, nameof(MediumPictureName)) ]
        [MemberNotNullWhen(true, nameof(LargePictureName)) ]
        public bool IsPicturesProcessed => PicturesProcessingStatus == ProcessingStatus.Completed;

        [NotMapped]
        [MemberNotNullWhen(true, nameof(AudioFolderName)) ]
        public bool IsAudioProcessed => AudioTranscodeProcessingStatus == ProcessingStatus.Completed;
    }
}