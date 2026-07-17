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
        [MaxLength(50)]
        public required string Title { get; set; }

        [Required]
        [MaxLength(50)]
        public required string NormalizedTitle { get; set; }

        [Required]
        public int Duration { get; set; }

        public long? OwnerUserId { get; set; }

        [Required]
        public TrackSource Source { get; set; } = TrackSource.Local;

        [MaxLength(16)]
        public string? ExternalId { get; set; }

        [Required]
        public LifeCycleStatus LifeCycleStatus { get; set; } = LifeCycleStatus.Active;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public TrackPictures Pictures { get; set; } = new();

        public TrackAudio Audio { get; set; } = new();

        [ForeignKey(nameof(OwnerUserId))]
        public User? Owner { get; set; }

        public ICollection<UserHasTrack> UserTracks { get; set; } = new List<UserHasTrack>();

        public ICollection<PlayListHasTrack> PlayListTracks { get; set; } = new List<PlayListHasTrack>();

        public ICollection<ListeningHistory> ListeningHistories { get; set; } = new List<ListeningHistory>();

        public ICollection<TrackArtist> TrackArtists { get; set; } = new List<TrackArtist>();
    }
}