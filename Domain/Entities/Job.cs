using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
    [Table("Job")]
    public class Job
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required JobType JobType { get; set; }

        [Required]
        [MaxLength(512)]
        public required string Payload { get; set; }

        [Required]
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
    }
}