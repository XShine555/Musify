using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    public class UserAlbum : Album
    {
        [Required]
        public required long OwnerUserId { get; set; }

        [MaxLength(64)]
        public string? ExternalId { get; set; }

        [ForeignKey(nameof(OwnerUserId))]
        public User Owner { get; set; }
    }
}
