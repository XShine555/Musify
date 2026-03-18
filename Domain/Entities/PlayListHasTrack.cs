using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    [Table("PlayListHasTrack")]
    public class PlayListHasTrack
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required Guid PlayListId { get; set; }

        [Required]
        public required Guid TrackId { get; set; }

        [Required]
        public int Position { get; set; } = 0;

        [Required]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(PlayListId)) ]
        public PlayList PlayList { get; set; }

        [ForeignKey(nameof(TrackId)) ]
        public Track? Track { get; set; }
    }
}