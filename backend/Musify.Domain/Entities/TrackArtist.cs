using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    [Table("TrackArtist")]
    public class TrackArtist
    {
        [Required]
        public required Guid TrackId { get; set; }

        [Required]
        public required Guid ArtistId { get; set; }

        [Required]
        public int Position { get; set; } = 0;

        [ForeignKey(nameof(TrackId)) ]
        public Track Track { get; set; }

        [ForeignKey(nameof(ArtistId)) ]
        public Artist Artist { get; set; }
    }
}
