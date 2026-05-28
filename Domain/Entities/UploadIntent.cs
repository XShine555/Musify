using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities
{
    [Table("UploadIntents")]
    public class UploadIntent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required Guid UserId { get; set; }

        [Required, MaxLength(128)]
        public required string Bucket { get; set; }

        // Full S3 key (including prefix and object name)
        [Required, MaxLength(512)]
        public required string Key { get; set; }

        // Just the filename portion stored in Track/PlayList entities (e.g. "{guid}.webp")
        [Required, MaxLength(128)]
        public required string ObjectName { get; set; }

        [Required, MaxLength(128)]
        public required string ContentType { get; set; }

        public long? ExpectedSizeBytes { get; set; }

        [Required]
        public UploadIntentPurpose Purpose { get; set; }

        [Required]
        public UploadIntentStatus Status { get; set; } = UploadIntentStatus.Issued;

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId)) ]
        public User User { get; set; } = null!;
    }
}
