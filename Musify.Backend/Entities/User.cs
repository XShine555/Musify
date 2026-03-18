using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
    [Table("User")]
    public class User
    {
        [Key]
        public required Guid Id { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public ICollection<Track> Tracks { get; set; } = new List<Track>();

        public ICollection<PlayList> PlayLists { get; set; } = new List<PlayList>();
    }
}