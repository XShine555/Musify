using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
        [MaxLength(200)]
        public required string Title { get; set; }

        [Required]
        [MaxLength(200)]
        public required string NormalizedTitle { get; set; }

        [Required]
        public double DurationSeconds { get; set; }

        [Required]
        public required long OwnerUserId { get; set; }

        [ForeignKey(nameof(OwnerUserId))]
        public required User Owner { get; set; }

        [Required]
        public LifeCycleStatus LifeCycleStatus { get; set; } = LifeCycleStatus.Active;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public TrackPictures Pictures { get; set; } = new();

        public TrackAudio Audio { get; set; } = new();

        public ICollection<UserHasTrack> UserTracks { get; set; } = new List<UserHasTrack>();

        public ICollection<PlayListHasTrack> PlayListTracks { get; set; } = new List<PlayListHasTrack>();

        public ICollection<AlbumHasTrack> AlbumTracks { get; set; } = new List<AlbumHasTrack>();

        public ICollection<ListeningHistory> ListeningHistories { get; set; } = new List<ListeningHistory>();

        public ICollection<TrackTag> Tags { get; set; } = new List<TrackTag>();
    }
}
