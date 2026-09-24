using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
    [Table("AlbumHasTrack")]
    public class AlbumHasTrack
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required Guid AlbumId { get; set; }

        public required Guid TrackId { get; set; }

        public int TrackNumber { get; set; } = 1;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(AlbumId))]
        public Album Album { get; set; } = null!;

        [ForeignKey(nameof(TrackId))]
        public Track Track { get; set; } = null!;
    }
}
