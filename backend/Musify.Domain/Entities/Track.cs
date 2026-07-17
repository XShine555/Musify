using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Musify.Domain.Abstractions;
using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    public class Track : IAuditable
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
        public int Duration { get; set; }

        [MaxLength(200)]
        public string? Artist { get; set; }

        public long? OwnerUserId { get; set; }

        [Required]
        public TrackSource Source { get; set; } = TrackSource.Local;

        [MaxLength(16)]
        public string? ExternalId { get; set; }

        [Required]
        public bool DownloadRequested { get; set; } = false;

        [MaxLength(64)]
        public string? OriginalPictureName { get; set; }

        [MaxLength(64)]
        public string? SmallPictureName { get; set; }

        [MaxLength(64)]
        public string? MediumPictureName { get; set; }

        [MaxLength(64)]
        public string? LargePictureName { get; set; }

        [MaxLength(64)]
        public string? OriginalAudioName { get; set; }

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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(OwnerUserId))]
        public User? Owner { get; set; }

        public ICollection<UserHasTrack> UserTracks { get; set; } = new List<UserHasTrack>();

        public ICollection<PlayListHasTrack> PlayListTracks { get; set; } = new List<PlayListHasTrack>();

        public ICollection<ListeningHistory> ListeningHistories { get; set; } = new List<ListeningHistory>();

        public ICollection<TrackArtist> TrackArtists { get; set; } = new List<TrackArtist>();

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