using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
    public class LocalTrack : Track
    {
        [Required]
        public required long OwnerUserId { get; set; }

        [ForeignKey(nameof(OwnerUserId)) ]
        public required User Owner { get; set; }
    }
}
