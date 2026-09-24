using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities;

[Table("UploadIntents")]
public class UploadIntent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required long UserId { get; set; }

    [Required, MaxLength(128)]
    public required string Bucket { get; set; }

    [Required, MaxLength(512)]
    public required string Key { get; set; }

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

    [NotMapped]
    public bool IsConsumed => Status == UploadIntentStatus.Consumed;

    [NotMapped]
    public bool IsExpired => Status == UploadIntentStatus.Expired || ExpiresAt < DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
