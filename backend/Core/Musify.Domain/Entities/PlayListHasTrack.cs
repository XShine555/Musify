using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
    [Table("PlayListHasTrack")]
    public class PlayListHasTrack
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required Guid PlayListId { get; set; }

        public required Guid TrackId { get; set; }

        public int Position { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(PlayListId))]
        public PlayList PlayList { get; set; } = null!;

        [ForeignKey(nameof(TrackId))]
        public Track Track { get; set; } = null!;
    }
}
