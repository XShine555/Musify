using System.ComponentModel.DataAnnotations;
using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities
{
    public class ExternalTrack : Track
    {
        [Required]
        public required TrackSource Source { get; set; }

        [Required]
        [MaxLength(64)]
        public required string ExternalId { get; set; }

        public ICollection<TrackArtist> TrackArtists { get; set; } = new List<TrackArtist>();
    }
}
