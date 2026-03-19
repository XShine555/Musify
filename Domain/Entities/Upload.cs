using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
    [Table("Upload")]
    public class Upload
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required string KeyName { get; set; }

        [Required]
        public required string BucketName { get; set; }

        [Required]
        public required string ContentType { get; set; }

        [Required]
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        [Required]
        public UploadState State { get; set; } = UploadState.Pending;
    }
}