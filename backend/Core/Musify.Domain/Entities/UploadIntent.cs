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

        public required long UserId { get; set; }

        [MaxLength(128)]
        public required string Bucket { get; set; }

        [MaxLength(512)]
        public required string Key { get; set; }

        [MaxLength(128)]
        public required string ObjectName { get; set; }

        [MaxLength(128)]
        public required string ContentType { get; set; }

        public long? ExpectedSizeBytes { get; set; }

        public UploadIntentPurpose Purpose { get; set; }

        public UploadIntentStatus Status { get; set; } = UploadIntentStatus.Issued;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public bool IsConsumed => Status == UploadIntentStatus.Consumed;

        public bool IsExpiredAt(DateTime now) => Status == UploadIntentStatus.Expired || ExpiresAt < now;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
