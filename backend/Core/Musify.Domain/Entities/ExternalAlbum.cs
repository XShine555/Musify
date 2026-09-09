using System.ComponentModel.DataAnnotations;
using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities
{
    public class ExternalAlbum : Album
    {
        [Required]
        public required TrackSource Source { get; set; }

        [Required]
        [MaxLength(64)]
        public required string ExternalId { get; set; }

        [MaxLength(512)]
        public string? ThumbnailUrl { get; set; }
    }
}
