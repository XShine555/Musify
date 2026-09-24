using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities;

[Table("Upload")]
public class Upload
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required string Bucket { get; set; }

    [Required]
    public required string Key { get; set; }

    [Required]
    public required string ContentType { get; set; }

    [Required]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    [Required]
    public UploadState State { get; set; } = UploadState.Pending;
}
